using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Countries;
using SchatzApp.Entities;

namespace SchatzApp.Logic
{
    /// <summary>
    /// Serves all REST API requests, including single-page app's internal content requests.
    /// </summary>
    public class ApiController : Controller
    {
        /// <summary>
        /// This controller's logger.
        /// </summary>
        private readonly ILogger logger;
        /// <summary>
        /// Resolves remote IP address into country code.
        /// </summary>
        private readonly CountryResolver countryResolver;
        /// <summary>
        /// Provides content HTML based on relative URL of singe-page request.
        /// </summary>
        private readonly PageProvider pageProvider;
        /// <summary>
        /// Provides random samples for the vocab quiz.
        /// </summary>
        private readonly Sampler sampler;
        /// <summary>
        /// Repository of submitted quizzes.
        /// </summary>
        private readonly ResultRepo resultRepo;
        /// <summary>
        /// Secret that caller must know for all DB dump and file fetch calls.
        /// </summary>
        private readonly string exportSecret;
        /// <summary>
        /// Base path where export files go.
        /// </summary>
        private readonly string exportPath;

        /// <summary>
        /// Ctor: inject dependencies.
        /// </summary>
        public ApiController(ILoggerFactory lf,
            CountryResolver countryResolver, PageProvider pageProvider,
            Sampler sampler, ResultRepo resultRepo, IConfiguration config)
        {
            logger = lf.CreateLogger(GetType().FullName);
            this.countryResolver = countryResolver;
            this.pageProvider = pageProvider;
            this.sampler = sampler;
            this.resultRepo = resultRepo;
            exportSecret = config["exportSecret"];
            exportPath = config["exportPath"];
        }

        /// <summary>
        /// Ughly: handcrafted HTML (XML) string escaping.
        /// </summary>
        private static string esc(string str, bool quotes = false)
        {
            str = str.Replace("&", "&amp;");
            str = str.Replace("<", "&lt;");
            str = str.Replace(">", "&gt;");
            if (quotes)
            {
                str = str.Replace("'", "&apos;");
                str = str.Replace("\"", "&quot;");
            }
            return str;
        }

        /// <summary>
        /// Serves single-page app's dynamic content requests for in-page navigation.
        /// </summary>
        /// <param name="rel">Relative URL of content.</param>
        public IActionResult GetPage([FromForm] string rel)
        {
            var pi = pageProvider.GetPage(rel, false);
            if (pi == null) pi = pageProvider.GetPage("404", false);
            PageResult res = new PageResult
            {
                NoIndex = pi.NoIndex,
                Title = pi.Title,
                Description = pi.Description,
                Keywords = pi.Keywords,
                Html = pi.Html
            };
            return new ObjectResult(res);
        }

        /// <summary>
        /// Serves random sample for populating a quiz.
        /// </summary>
        public IActionResult GetQuiz()
        {
            string[] sample1, sample2;
            sampler.GetPermutatedSample(out sample1, out sample2);
            QuizResult res = new QuizResult
            {
                Words1 = sample1,
                Words2 = sample2
            };
            return new ObjectResult(res);
        }

        /// <summary>
        /// Evaluates quiz and survey; stores results; returns ID so client can navigate to results page.
        /// </summary>
        /// <param name="quiz">The user's quiz choices, JSON serialized in quiz.js.</param>
        /// <param name="survey">The user's survey input (whatever was provided), JSON serialized in quiz.js.</param>
        /// <returns></returns>
        public IActionResult EvalQuiz([FromForm] string quiz, [FromForm] string survey,
            [FromForm] string quizCount, [FromForm] string surveyCount)
        {
            // Parse data from query
            var oQuiz = JsonConvert.DeserializeObject<IList<string[]>>(quiz);
            var oSurvey = JsonConvert.DeserializeObject<SurveyData>(survey);
            // Have sampler evaluate result
            int score;
            char[] resCoded;
            sampler.Eval(oQuiz, out score, out resCoded);
            // Get country from remote IP. Trickier b/c of NGINX reverse proxy.
            string country;
            string xfwd = HttpContext.Request.Headers["X-Real-IP"];
            if (xfwd != null) country = countryResolver.GetContryCode(IPAddress.Parse(xfwd));
            else country = countryResolver.GetContryCode(HttpContext.Connection.RemoteIpAddress);
            // Store result
            int nQuizCount = int.Parse(quizCount);
            int nSurveyCount = int.Parse(surveyCount);
            StoredResult sr = new StoredResult(country, DateTime.Now, nQuizCount, nSurveyCount, score, new string(resCoded), oSurvey);
            string uid = resultRepo.StoreResult(sr);
            // Response is result code, pure and simple.
            return new ObjectResult(uid);
        }

        /// <summary>
        /// Retrieves score based on quiz's unique ID.
        /// </summary>
        public IActionResult GetScore([FromForm] string uid)
        {
            // Retrieve score
            int score = resultRepo.LoadScore(uid.Substring(0, 10));
            // Respond
            if (score == -1) return new ObjectResult(null);
            return new ObjectResult(score);
        }

        /// <summary>
        /// Cleans up excess dump files from the past; renames latest one to "results.txt"
        /// </summary>
        /// <param name="currFN"></param>
        private void dumpCleanup(string currFN)
        {

        }

        /// <summary>
        /// Starts database dump in BG thread and returns immediately.
        /// </summary>
        public IActionResult Export([FromQuery] string secret)
        {
            // Not for anyone.
            if (secret != exportSecret) return new ObjectResult("barf");
            // Dump file name: current date and time
            string fname = "results-{0}-{1}-{2}!{3}-{4}.txt";
            DateTime dt = DateTime.Now;
            fname = string.Format(fname, dt.Year, dt.Month.ToString("00"), dt.Day.ToString("00"), dt.Hour.ToString("00"), dt.Minute.ToString("00"));
            fname = Path.Combine(exportPath, fname);
            // Start process async
            bool ok = resultRepo.DumpToFileAsync(fname);
            // Return: ok or not
            return new ObjectResult(ok ? "started" : "dump-already-in-progress");
        }
    }
}
