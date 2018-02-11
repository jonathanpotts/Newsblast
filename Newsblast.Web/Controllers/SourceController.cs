using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Newsblast.Web.Controllers
{
    public class SourceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}