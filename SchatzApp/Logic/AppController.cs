using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SchatzApp.Logic
{
    public class AppController : Controller
    {
        public IActionResult Index(string paras)
        {
            PageResult pr = ApiController.GetPageResult(paras);
            return View("/AppIndex.cshtml", pr);
        }
    }
}
