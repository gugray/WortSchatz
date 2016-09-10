using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using SchatzApp.Entities;

namespace SchatzApp.Logic
{
    /// <summary>
    /// Serves all REST API requests, including single-page app's internal content requests.
    /// </summary>
    public class ApiController : Controller
    {
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
        /// Ctor: inject dependencies.
        /// </summary>
        public ApiController(PageProvider pageProvider, Sampler sampler, ResultRepo resultRepo)
        {
            this.pageProvider = pageProvider;
            this.sampler = sampler;
            this.resultRepo = resultRepo;
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
            var pi = pageProvider.GetPage(rel);
            if (pi == null) return new ObjectResult(null);
            PageResult res = new PageResult
            {
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
        public IActionResult EvalQuiz([FromForm] string quiz, [FromForm] string survey)
        {
            // Parse data from query
            var oQuiz = JsonConvert.DeserializeObject<IList<string[]>>(quiz);
            var oSurvey = JsonConvert.DeserializeObject<SurveyData>(survey);
            // Have sampler evaluate result
            int score;
            char[] resCoded;
            sampler.Eval(oQuiz, out score, out resCoded);
            // TO-DO: country from IP
            // Request.HttpContext etc
            // Store result
            StoredResult sr = new StoredResult("NNN", DateTime.Now, 0, 0, score, new string(resCoded), oSurvey);
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
    }
}
