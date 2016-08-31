using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace SchatzApp
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

        public IActionResult GetGammaQuiz([FromForm] string name)
        {
            string[] sample = Sampler.Instance.GetPermutatedSample();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i != sample.Length; ++i)
            {
                if (i % 4 == 0)
                {
                    if (i != 0) sb.AppendLine("</div>");
                    sb.AppendLine("<div class='quizRow'>");
                }
                sb.AppendLine("<div class='quizCell'>" + esc(sample[i]) + "</div>");
            }
            sb.AppendLine("</div>");
            return new ObjectResult(sb.ToString());
        }

        public IActionResult EvalQuiz([FromForm] string name, [FromForm] string[] words)
        {
            QuizResult res = new QuizResult();
            int scoreProp, scoreMean;
            Sampler.Instance.Eval(name, words, out scoreProp, out scoreMean);
            res.ScoreProp = Sampler.RoundTo(scoreProp, 500);
            res.ScoreMean = Sampler.RoundTo(scoreMean, 500);
            return new ObjectResult(res);
        }
    }
}
