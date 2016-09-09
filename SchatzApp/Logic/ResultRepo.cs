using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

using SchatzApp.Entities;

namespace SchatzApp.Logic
{
    /// <summary>
    /// Stores and retrieves quiz results.
    /// </summary>
    public class ResultRepo : IDisposable
    {
        /// <summary>
        /// My own logger.
        /// </summary>
        private readonly ILogger logger;
        /// <summary>
        /// Single connection maintained throughout instance's lifetime.
        /// </summary>
        private readonly SqliteConnection conn;

        private const string sqlInitDB = @"
            CREATE TABLE results(
                uid         INT NOT NULL,
                country     TEXT NOT NULL,
                quiz_ix     INT NOT NULL,
                survey_ix   INT NOT NULL,
                score       INT NOT NULL,
                res_enc     TEXT NOT NULL,
                survey_enc  TEXT
            );
            CREATE UNIQUE INDEX idx_results_uid ON results(uid);";

        private const string sqlSelectMaxDayId = @"
            SELECT MAX(uid) FROM results
            WHERE uid >= @lo AND uid < @hi;";
        private SqliteCommand cmdSelectMaxDayId = null;

        private const string sqlInsertResult = @"
            INSERT INTO results (uid, country, quiz_ix, survey_ix, score, res_enc, survey_enc)
            VALUES (@uid, @country, @quiz_ix, @survey_ix, @score, @res_enc, @survey_enc);";
        private SqliteCommand cmdInsertResult = null;

        private const string sqlSelResult = @"
            SELECT uid, country, quiz_ix, survey_ix, score, res_enc, survey_enc
            FROM results WHERE uid=@uid;";
        private SqliteCommand cmdSelResult = null;

        private const string sqlSelScore = @"SELECT score FROM results WHERE uid=@uid;";
        private SqliteCommand cmdSelScore = null;

        /// <summary>
        /// Ctor: open existing DB file, or create new one from scratch.
        /// </summary>
        public ResultRepo(ILoggerFactory lf, string dbFileName)
        {
            logger = lf.CreateLogger(GetType().FullName);
            logger.LogInformation("Results repository initializing...");

            // SQLite is unhappy about Linux-style relative paths, which we want to use in development environment
            // This conversion to absolute path works around that.
            string dbFileNameAbs = Path.GetFullPath(dbFileName);
            bool dbExists = File.Exists(dbFileNameAbs);
            SqliteConnectionStringBuilder csb = new SqliteConnectionStringBuilder
            {
                DataSource = dbFileNameAbs,
                Mode = SqliteOpenMode.ReadWriteCreate
            };
            conn = new SqliteConnection();
            conn.ConnectionString = csb.ConnectionString;
            conn.Open();
            if (!dbExists) initDB();
            prepareCommands();

            logger.LogInformation("Results repository initialized.");
        }

        /// <summary>
        /// Sets up table(s) and index(es) in brand-new database.
        /// </summary>
        private void initDB()
        {
            logger.LogInformation("Initializing results database from scratch.");
            using (SqliteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = sqlInitDB;
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Prepares SQL commands that the instance will be reusing.
        /// </summary>
        private void prepareCommands()
        {
            cmdSelectMaxDayId = conn.CreateCommand();
            cmdSelectMaxDayId.CommandText = sqlSelectMaxDayId;
            cmdSelectMaxDayId.Parameters.Add("@lo", SqliteType.Integer);
            cmdSelectMaxDayId.Parameters.Add("@hi", SqliteType.Integer);
            cmdSelectMaxDayId.Prepare();

            cmdInsertResult = conn.CreateCommand();
            cmdInsertResult.CommandText = sqlInsertResult;
            cmdInsertResult.Parameters.Add("@uid", SqliteType.Integer);
            cmdInsertResult.Parameters.Add("@country", SqliteType.Text);
            cmdInsertResult.Parameters.Add("@quiz_ix", SqliteType.Integer);
            cmdInsertResult.Parameters.Add("@survey_ix", SqliteType.Integer);
            cmdInsertResult.Parameters.Add("@score", SqliteType.Integer);
            cmdInsertResult.Parameters.Add("@res_enc", SqliteType.Text);
            cmdInsertResult.Parameters.Add("@survey_enc", SqliteType.Text);
            cmdInsertResult.Prepare();

            cmdSelResult = conn.CreateCommand();
            cmdSelResult.CommandText = sqlSelResult;
            cmdSelResult.Parameters.Add("@uid", SqliteType.Integer);
            cmdSelResult.Prepare();

            cmdSelScore = conn.CreateCommand();
            cmdSelScore.CommandText = sqlSelScore;
            cmdSelScore.Parameters.Add("@uid", SqliteType.Integer);
            cmdSelScore.Prepare();
        }

        /// <summary>
        /// Stores a new quiz+survey result in the DB.
        /// </summary>
        /// <param name="sr">The data to store.</param>
        /// <returns>The stored item's ID, encoded as a 10-character alphanumeric string.</returns>
        public string StoreResult(StoredResult sr)
        {
            long uid = 0;
            lock (conn)
            {
                SqliteTransaction trans = null;
                try
                {
                    trans = conn.BeginTransaction();
                    uid = getNextUid(sr.Date, trans);
                    storeResult(uid, trans, sr);
                    trans.Commit(); trans.Dispose(); trans = null;
                }
                catch (Exception ex)
                {
                    logger.LogError(new EventId(), ex, "Failed to store quiz results.");
                    if (trans != null) trans.Rollback();
                    throw;
                }
                finally { if (trans != null) trans.Dispose(); }
            }
            return uidToString(uid);
        }

        /// <summary>
        /// Retrieves stored score by 10-character alphanumeric ID.
        /// </summary>
        /// <param name="uidStr">The result's UID.</param>
        /// <returns>The stored score, or -1 if not found.</returns>
        public int LoadScore(string uidStr)
        {
            long uid = stringToUid(uidStr);
            //int dateNum = (int)(uid / 1000000);
            //int xDay = dateNum % 100; dateNum /= 100;
            //int xMonth = dateNum % 100; dateNum /= 100;
            //int xYear = dateNum;
            //DateTime date = new DateTime(xYear, xMonth, xDay);
            lock (conn)
            {
                cmdSelScore.Parameters["@uid"].Value = uid;
                using (var rdr = cmdSelScore.ExecuteReader())
                {
                    while (rdr.Read()) { return rdr.GetInt32(0); }
                }
            }
            return -1;
        }

        /// <summary>
        /// Helper for long<>Alpha10 conversion.
        /// </summary>
        private static char numToChar(long num)
        {
            if (num < 26) return (char)(num + 'a');
            if (num < 52) return (char)(num - 26 + 'A');
            throw new Exception("Invalid conversion to char: " + num.ToString());
        }

        /// <summary>
        /// Helper for long<>Alpha10 conversion.
        /// </summary>
        private static long charToNum(char c)
        {
            if (c >= 'a' && c <= 'z') return c - 'a';
            if (c >= 'A' && c <= 'Z') return c - 'A' + 26;
            throw new Exception("Invalid conversion to number: #" + ((int)c).ToString());
        }

        /// <summary>
        /// Converts long UID to Alpha10.
        /// </summary>
        private static string uidToString(long uid)
        {
            // xx0x00xx0x
            char[] res = new char[10];
            long x = uid;
            res[9] = numToChar(x % 52); x /= 52;
            res[8] = (char)((x % 10) + '0'); x /= 10;
            res[7] = numToChar(x % 52); x /= 52;
            res[6] = numToChar(x % 52); x /= 52;
            res[5] = (char)((x % 10) + '0'); x /= 10;
            res[4] = (char)((x % 10) + '0'); x /= 10;
            res[3] = numToChar(x % 52); x /= 52;
            res[2] = (char)((x % 10) + '0'); x /= 10;
            res[1] = numToChar(x % 52); x /= 52;
            res[0] = numToChar(x % 52); x /= 52;
            if (x != 0) throw new Exception("Number cannot be represented: " + uid.ToString());
            return new string(res);
        }

        /// <summary>
        /// Converts Alpha10 to long UID.
        /// </summary>
        private static long stringToUid(string code)
        {
            // xx0x00xx0x
            if (code.Length != 10) throw new Exception("Invalid representation: " + code);
            long res = 0;
            res += charToNum(code[0]) * ((long)52 * 10 * 52 * 10 * 10 * 52 * 52 * 10 * 52);
            res += charToNum(code[1]) * ((long)10 * 52 * 10 * 10 * 52 * 52 * 10 * 52);
            res += (code[2] - '0') * ((long)52 * 10 * 10 * 52 * 52 * 10 * 52);
            res += charToNum(code[3]) * ((long)10 * 10 * 52 * 52 * 10 * 52);
            res += (code[4] - '0') * ((long)10 * 52 * 52 * 10 * 52);
            res += (code[5] - '0') * ((long)52 * 52 * 10 * 52);
            res += charToNum(code[6]) * ((long)52 * 10 * 52);
            res += charToNum(code[7]) * ((long)10 * 52);
            res += (code[8] - '0') * ((long)52);
            res += charToNum(code[9]);
            return res;
        }

        /// <summary>
        /// Executes "store" SQL command within transaction.
        /// </summary>
        private void storeResult(long uid, SqliteTransaction trans, StoredResult sr)
        {
            cmdInsertResult.Transaction = trans;
            cmdInsertResult.Parameters["@uid"].Value = uid;
            cmdInsertResult.Parameters["@country"].Value = sr.CountryCode;
            cmdInsertResult.Parameters["@quiz_ix"].Value = sr.PrevQuizCount;
            cmdInsertResult.Parameters["@survey_ix"].Value = sr.PrevSurveyCount;
            cmdInsertResult.Parameters["@score"].Value = sr.Score;
            cmdInsertResult.Parameters["@res_enc"].Value = sr.EncodedResult;
            cmdInsertResult.Parameters["@survey_enc"].Value = sr.GetEncodedSurvey();
            cmdInsertResult.ExecuteNonQuery();
        }

        /// <summary>
        /// Gets next available UID for current day; executed within transaction.
        /// </summary>
        private long getNextUid(DateTime dtNow, SqliteTransaction trans)
        {
            long lo = dtNow.Year * 10000 + dtNow.Month * 100 + dtNow.Day;
            lo *= 1000000;
            DateTime dtTomorrow = dtNow.AddDays(1);
            long hi = dtTomorrow.Year * 10000 + dtTomorrow.Month * 100 + dtTomorrow.Day;
            hi *= 1000000;
            cmdSelectMaxDayId.Transaction = trans;
            cmdSelectMaxDayId.Parameters["@lo"].Value = lo;
            cmdSelectMaxDayId.Parameters["@hi"].Value = hi;
            object dbMax = cmdSelectMaxDayId.ExecuteScalar();
            // No results for this date yet
            if (dbMax == null || dbMax is DBNull) return lo;
            // Increment largest for this date
            return (long)dbMax + 1;
        }

        /// <summary>
        /// Releases SQLite entities owned by this instance.
        /// </summary>
        public void Dispose()
        {
            logger.LogInformation("Results repository disposing...");

            if (cmdSelScore != null) cmdSelScore.Dispose();
            if (cmdSelResult != null) cmdSelResult.Dispose();
            if (cmdInsertResult != null) cmdInsertResult.Dispose();
            if (cmdSelectMaxDayId != null) cmdSelectMaxDayId.Dispose();
            if (conn != null) { conn.Close(); conn.Dispose(); }
        }
    }
}
