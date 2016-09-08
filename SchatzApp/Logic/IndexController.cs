using Microsoft.AspNetCore.Mvc;

using SchatzApp.Entities;

namespace SchatzApp.Logic
{
    /// <summary>
    /// Serves page of single-page app (we only have one page).
    /// </summary>
    public class IndexController : Controller
    {
        /// <summary>
        /// Provides content HTML based on relative URL of request.
        /// </summary>
        private readonly PageProvider pageProvider;

        /// <summary>
        /// Ctor: infuse dependencies.
        /// </summary>
        public IndexController(PageProvider pageProvider)
        {
            this.pageProvider = pageProvider;
        }

        /// <summary>
        /// Serves single-page app's page requests.
        /// </summary>
        /// <param name="paras">The entire relative URL.</param>
        public IActionResult Index(string paras)
        {
            var pi = pageProvider.GetPage(paras);
            if (pi == null) return null;
            PageResult res = new PageResult
            {
                Title = pi.Title,
                Description = pi.Description,
                Keywords = pi.Keywords,
                Html = pi.Html
            };
            return View("/Index.cshtml", res);
        }
    }
}
