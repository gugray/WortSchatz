using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace SchatzApp.Logic
{
    public partial class ApiController : Controller
    {
        private readonly PageProvider pageProvider;
        private readonly Sampler sampler;

        public ApiController(PageProvider pageProvider, Sampler sampler)
        {
            this.pageProvider = pageProvider;
            this.sampler = sampler;
        }

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

        public IActionResult GetPage([FromForm] string rel)
        {
            var pi = pageProvider.GetPage(rel);
            if (pi == null) return null;
            PageResult res = new PageResult
            {
                Title = pi.Title,
                Description = pi.Description,
                Keywords = pi.Keywords,
                Html = pi.Html
            };
            return new ObjectResult(res);
        }

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

        private class SurveyData
        {
            public string Native;
            public string Age;
            public string NativeCountry;
            public string NativeEducation;
            public string NativeOtherLangs;
            public string NnCountryNow;
            public string NnGermanTime;
            public string NnGermanLevel;
        }

        public IActionResult EvalQuiz([FromForm] string quiz, [FromForm] string survey)
        {
            var oQuiz = JsonConvert.DeserializeObject<IList<string[]>>(quiz);
            var oSurvey = JsonConvert.DeserializeObject<SurveyData>(survey);
            int score;
            char[] resCoded;
            sampler.Eval(oQuiz, out score, out resCoded);
            if (score > 18000) score = Sampler.RoundTo(score, 500);
            else score = Sampler.RoundTo(score, 200);
            // TO-DO: store results; return URL of results page
            return new ObjectResult(score);
        }
    }
}
