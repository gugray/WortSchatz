using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

namespace SchatzApp.Logic
{
    public partial class ApiController : Controller
    {
        private readonly IHostingEnvironment env;

        public ApiController(IHostingEnvironment env)
        {
            this.env = env;
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

        public static PageResult GetPageResult(string rel)
        {
            if (rel == null) rel = "/";
            else
            {
                rel = rel.TrimEnd('/');
                if (rel == string.Empty) rel = "/";
                if (!rel.StartsWith("/")) rel = "/" + rel;
            }
            var pi = PageProvider.Instant.GetPage("de", rel);
            if (pi == null) return null;
            PageResult res = new PageResult
            {
                Title = pi.Title,
                Description = pi.Description,
                Keywords = pi.Keywords,
                Html = pi.Html
            };
            return res;
        }

        public IActionResult GetPage([FromForm] string rel)
        {
            return new ObjectResult(GetPageResult(rel));
        }

        public IActionResult GetQuiz()
        {
            string[] sample1, sample2;
            Sampler.Instance.GetPermutatedSample(out sample1, out sample2);
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
            Sampler.Instance.Eval(oQuiz, out score, out resCoded);
            if (score > 18000) score = Sampler.RoundTo(score, 500);
            else score = Sampler.RoundTo(score, 200);
            // TO-DO: store results; return URL of results page
            return new ObjectResult(score);
        }
    }
}
