using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using SchatzApp.Entities;

namespace SchatzApp.Logic
{
    /// <summary>
    /// Serves page of single-page app (we only have one page).
    /// </summary>
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class IndexController : Controller
    {
        /// <summary>
        /// Provides content HTML based on relative URL of request.
        /// </summary>
        private readonly PageProvider pageProvider;
        /// <summary>
        /// Results repository.
        /// </summary>
        private readonly ResultRepo resultRepo;
        /// <summary>
        /// Google Analytics code.
        /// </summary>
        private readonly string gaCode;

        /// <summary>
        /// Ctor: infuse dependencies.
        /// </summary>
        public IndexController(PageProvider pageProvider, ResultRepo resultRepo, IConfiguration config)
        {
            this.pageProvider = pageProvider;
            this.resultRepo = resultRepo;
            gaCode = config["gaCode"];
        }

        /// <summary>
        /// Serves single-page app's page requests.
        /// </summary>
        /// <param name="paras">The entire relative URL.</param>
        public IActionResult Index(string paras)
        {
            var pi = pageProvider.GetPage(paras, true);
            if (pi == null) pi = pageProvider.GetPage("404", true);
            // If it's the results page, we cheat: retrieve score and fill title right here
            // Needed so Facebook shows my actual number when sharing
            string title = pi.Title;
            if (pi.RelNorm.StartsWith("/ergebnis/"))
            {
                string uid = pi.RelNorm.Replace("/ergebnis/", "");
                uid = uid.Substring(0, 10);
                int score = resultRepo.LoadScore(uid);
                title = title.Replace("*", score.ToString());
            }
            PageResult pr = new PageResult
            {
                Title = title,
                Description = pi.Description,
                Keywords = pi.Keywords,
                Html = pi.Html
            };
            IndexModel model = new IndexModel(pr, gaCode);
            return View("/Index.cshtml", model);
        }
    }
}
