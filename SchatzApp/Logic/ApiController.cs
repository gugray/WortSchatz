using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

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
            string[] sample = Sampler.Instance.GetPermutatedSample();
            QuizResult res = new QuizResult
            {
                Words1 = new string[20],
                Words2 = new string[40]
            };
            for (int i = 0; i != 20; ++i) res.Words1[i] = sample[i];
            for (int i = 0; i != 40; ++i) res.Words2[i] = sample[i + 20];
            return new ObjectResult(res);
        }

        public IActionResult EvalQuiz([FromForm] string name, [FromForm] string[] words)
        {
            EvalResult res = new EvalResult();
            int scoreProp, scoreMean;
            Sampler.Instance.Eval(name, words, out scoreProp, out scoreMean);
            res.ScoreProp = Sampler.RoundTo(scoreProp, 500);
            res.ScoreMean = Sampler.RoundTo(scoreMean, 500);
            return new ObjectResult(res);
        }
    }
}
