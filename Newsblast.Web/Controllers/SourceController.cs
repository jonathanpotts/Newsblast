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