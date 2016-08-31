using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SchatzApp
{
    public class AppController : Controller
    {
        public IActionResult Index(string paras)
        {
            AppModel model = new AppModel();
            return View("/AppIndex.cshtml", model);
        }
    }
}
