using System;
using System.IO;
using System.Threading;
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
        /// Absolute path to DB file name.
        /// </summary>
        private readonly string dbFileNameAbs;
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

        private const string sqlSelAll = @"
            SELECT uid, country, quiz_ix, survey_ix, score, res_enc, survey_enc
            FROM results ORDER BY uid ASC;";

        private const string sqlSelScore = @"SELECT score FROM results WHERE uid=@uid;";
        private SqliteCommand cmdSelScore = null;

        /// <summary>
        /// Ctor: open existing DB file, or create new one from scratch.
        /// </summary>
        public ResultRepo(ILoggerFactory lf, string dbFileName)
        {
            if (lf != null) logger = lf.CreateLogger(GetType().FullName);
            else logger = new DummyLogger();
            logger.LogInformation("Results repository initializing...");

            // SQLite is unhappy about Linux-style relative paths, which we want to use in development environment
            // This conversion to absolute path works around that.
            dbFileNameAbs = Path.GetFullPath(dbFileName);
            bool dbExists;
            getOpenConn(out conn, out dbExists);
            if (!dbExists) initDB();
            prepareCommands();

            logger.LogInformation("Results repository initialized.");
        }

        /// <summary>
        /// Returns a new, open connection to the DB.
        /// </summary>
        private void getOpenConn(out SqliteConnection res, out bool dbExists)
        {
            dbExists = File.Exists(dbFileNameAbs);
            SqliteConnectionStringBuilder csb = new SqliteConnectionStringBuilder
            {
                DataSource = dbFileNameAbs,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            };
            res = new SqliteConnection();
            res.ConnectionString = csb.ConnectionString;
            res.Open();
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
            return StoreBatch(sr, 1);
        }

        /// <summary>
        /// Diagnostic function: stores same item N times in a single transaction. Used to grow DB in stress test.
        /// </summary>
        /// <returns>UID of last stored item.</returns>
        public string StoreBatch(StoredResult sr, int count)
        {
            long uid = 0;
            lock (conn)
            {
                SqliteTransaction trans = null;
                try
                {
                    trans = conn.BeginTransaction();
                    for (int i = 0; i != count; ++i)
                    {
                        uid = getNextUid(sr.Date, trans);
                        storeResult(uid, trans, sr);
                    }
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
        /// Rounds number to the nearest 100, 200, 500 etc. Helper for showing less-precise results in UI.
        /// </summary>
        private static int roundScore(int rawScore)
        {
            int prec = 500;
            if (rawScore < 18000) prec = 200;
            if (rawScore < 9000) prec = 100;
            return (int)(Math.Round((double)rawScore / prec) * prec);
        }

        /// <summary>
        /// Retrieves stored score by 10-character alphanumeric ID.
        /// </summary>
        /// <param name="uidStr">The result's UID.</param>
        /// <returns>The stored score, or -1 if not found.</returns>
        public int LoadScore(string uidStr)
        {
            long uid = stringToUid(uidStr);
            lock (conn)
            {
                cmdSelScore.Parameters["@uid"].Value = uid;
                using (var rdr = cmdSelScore.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        int score = rdr.GetInt32(0);
                        return roundScore(score);
                    }
                }
            }
            return -1;
        }

        private object bgDumpFlagLock = new object();
        private bool bgDumpFlag = false;

        /// <summary>
        /// Dumps quiz/survey data to file in BG thread
        /// </summary>
        /// <returns>Fale if a dump is already in progress; true if new one is now started.</returns>
        public bool DumpToFileAsync(string tsvFileName)
        {
            lock (bgDumpFlagLock)
            {
                if (bgDumpFlag) return false;
                bgDumpFlag = true;
            }
            ThreadPool.QueueUserWorkItem(dumpThreadFun, tsvFileName);
            return true;
        }

        /// <summary>
        /// BG thread function that invokes dump; clears flag when done.
        /// </summary>
        private void dumpThreadFun(object o)
        {
            try { doDumpToFile((string)o); }
            catch (Exception ex) { logger.LogError(new EventId(), ex, "Background dump failed"); }
            finally
            {
                lock (bgDumpFlagLock) { bgDumpFlag = false; }
            }
        }

        /// <summary>
        /// Dumps entire repository into TSV file.
        /// </summary>
        public void DumpToFile(string tsvFileName)
        {
            doDumpToFile(tsvFileName);
        }

        /// <summary>
        /// Performs actual dump.
        /// </summary>
        private void doDumpToFile(string tsvFileName)
        {
            SqliteConnection dumpConn = null;
            SqliteCommand cmdSelAll = null;
            try
            {
                // For the dump, we use a separate connection
                // This allows main connection to be safely accessed from other threads while dump is in progress
                bool dbExists;
                getOpenConn(out dumpConn, out dbExists);

                using (SqliteCommand cmdPragma = dumpConn.CreateCommand())
                {
                    // This pragma is a MUST to avoid locking the DB for the duration of the dump
                    // Without it, concurrent store requests will time out.
                    cmdPragma.CommandText = "PRAGMA read_uncommitted = 1;";
                    cmdPragma.ExecuteNonQuery();
                }

                // Dumping is rare. No need to prepare and reuse this command.
                cmdSelAll = dumpConn.CreateCommand();
                cmdSelAll.CommandText = sqlSelAll;
                cmdSelAll.Prepare();

                // Write from reader, straight to output file.
                using (FileStream fs = new FileStream(tsvFileName, FileMode.Create, FileAccess.ReadWrite))
                using (StreamWriter sw = new StreamWriter(fs))
                using (var rdr = cmdSelAll.ExecuteReader())
                {
                    sw.WriteLine(StoredResult.GetTSVHeader());
                    while (rdr.Read())
                    {
                        // uid, country, quiz_ix, survey_ix, score, res_enc, survey_enc
                        int dateNum = (int)(rdr.GetInt64(0) / 1000000);
                        int xDay = dateNum % 100; dateNum /= 100;
                        int xMonth = dateNum % 100; dateNum /= 100;
                        int xYear = dateNum;
                        DateTime date = new DateTime(xYear, xMonth, xDay);
                        StoredResult sr = new StoredResult(rdr.GetString(1), date, rdr.GetInt32(2), rdr.GetInt32(3),
                            rdr.GetInt32(4), rdr.GetString(5), rdr.GetString(6));
                        sw.WriteLine(sr.GetTSV());
                    }
                }
                dumpConn.Close();
            }
            finally
            {
                if (cmdSelAll != null) cmdSelAll.Dispose();
                if (dumpConn != null) dumpConn.Dispose();
            }
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
            if (cmdInsertResult != null) cmdInsertResult.Dispose();
            if (cmdSelectMaxDayId != null) cmdSelectMaxDayId.Dispose();
            if (conn != null) { conn.Close(); conn.Dispose(); }
        }
    }
}
