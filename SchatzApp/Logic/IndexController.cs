using Microsoft.AspNetCore.Mvc;

namespace SchatzApp.Logic
{
    public class IndexController : Controller
    {
        private readonly PageProvider pageProvider;

        public IndexController(PageProvider pageProvider)
        {
            this.pageProvider = pageProvider;
        }

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
