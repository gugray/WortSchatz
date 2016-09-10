using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using SchatzApp.Entities;

namespace SchatzApp.Logic
{
    /// <summary>
    /// Details stored about one submitted quiz.
    /// </summary>
    public class StoredResult
    {
        /// <summary>
        /// Three-letter country code from request IP.
        /// </summary>
        public readonly string CountryCode;
        /// <summary>
        /// Year, month and day the quiz was submitted.
        /// </summary>
        public readonly DateTime Date;
        /// <summary>
        /// Number of times same user submitted a quiz prevously.
        /// </summary>
        public readonly int PrevQuizCount;
        /// <summary>
        /// Number of times same user submitted a quiz *with survey* previously.
        /// </summary>
        public readonly int PrevSurveyCount;
        /// <summary>
        /// Score on quiz.
        /// </summary>
        public readonly int Score;
        /// <summary>
        /// Full encoded result, as returned by <see cref="Sampler"/>.
        /// </summary>
        public readonly string EncodedResult;
        /// <summary>
        /// Answered survey questions as key-value pairs. Can be empty.
        /// </summary>
        private readonly Dictionary<string, string> survey = new Dictionary<string, string>();

        /// <summary>
        /// Gets survey value for provided key, or null.
        /// </summary>
        /// <remarks>
        /// Keys:
        /// Native (yes, no); Age (int-as-string)
        /// NativeCountry (de, at, ch, other)
        /// NativeEducation (none, grund-haupt, real-fach, gymnasium, fachhoch, bachelor, master, higher
        /// NativeOtherLangs (0, 2, 3, 4)
        /// NnCountryNow (yes, no)
        /// NnGermanTime (lessThan1M, 1to3M, 3to12M, 1to2Y, 2to5Y, 5to10Y, moreThan10Y)
        /// NnGermanLevel (A1, A2, B1, B2, C1, C2)
        /// </remarks>
        public string GetSurveyValue(string key)
        {
            if (!survey.ContainsKey(key)) return null;
            return survey[key];
        }

        /// <summary>
        /// Ctor: init immutable instance (when putting data into storage).
        /// </summary>
        public StoredResult(string countryCode, DateTime date,
            int prevQuizCount, int prevSurveyCount, int score,
            string encodedResult, SurveyData surveyData)
        {
            CountryCode = countryCode;
            Date = date;
            PrevQuizCount = prevQuizCount;
            PrevSurveyCount = prevSurveyCount;
            Score = score;
            EncodedResult = encodedResult;
            // Fill key-value map by reflecting on surveyData instance
            foreach (FieldInfo fi in typeof(SurveyData).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                string val = fi.GetValue(surveyData) as string;
                if (val == null || val == string.Empty) continue;
                survey[fi.Name] = val;
            }
        }

        /// <summary>
        /// Returns survey encoded as a single string ("key=value;other=blah").
        /// </summary>
        /// <returns></returns>
        public string GetEncodedSurvey()
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (var x in survey)
            {
                sb.Append(x.Key);
                sb.Append('=');
                sb.Append(x.Value);
                if (!first) sb.Append(';');
                else first = false;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Ctor: init immutable instance (when retrieving a stored result from DB).
        /// </summary>
        public StoredResult(string countryCode, DateTime date,
            int prevQuizCount, int prevSurveyCount, int score,
            string encodedResult, string surveyEncoded)
        {
            CountryCode = countryCode;
            Date = date;
            PrevQuizCount = prevQuizCount;
            PrevSurveyCount = prevSurveyCount;
            Score = score;
            EncodedResult = encodedResult;
            string[] parts = surveyEncoded.Split(';');
            foreach (string x in parts)
            {
                string[] kv = x.Split('=');
                if (kv.Length != 2) continue;
                survey[kv[0]] = kv[1];
            }
        }

        private const string txtTemplate = "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}";

        /// <summary>
        /// Gets header line for tab-separated export.
        /// </summary>
        public static string GetTSVHeader()
        {
            return string.Format(txtTemplate, "date", "country", "prev_quiz_cnt", "prev_survey_cnt",
                "score", "enc_res", "enc_survey");
        }

        /// <summary>
        /// Gets the result in tab-separated format for TXT export.
        /// </summary>
        public string GetTSV()
        {
            string dateStr = Date.Year.ToString("0000") + "-" + Date.Month.ToString("00") + "-" + Date.Day.ToString("00");
            return string.Format(txtTemplate, dateStr, CountryCode, PrevQuizCount, PrevSurveyCount,
                Score, EncodedResult, GetEncodedSurvey());
        }
    }
}
