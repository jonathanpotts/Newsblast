using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newsblast.Web.Models;

namespace Newsblast.Web.Controllers
{
    public class HomeController : NewsblastController
    {
        public IActionResult Index()
        {
            return View();
        }

        [Route("relay")]
        public IActionResult Relay(string url)
        {
            return Redirect(url);
        }

        [Route("error")]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("error/{statusCode}")]
        public IActionResult Error(int statusCode)
        {
            var error = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };

            switch (statusCode)
            {
                case (int)HttpStatusCode.Unauthorized:
                    return View("Unauthorized", error);

                case (int)HttpStatusCode.Forbidden:
                    return View("Forbidden", error);

                case (int)HttpStatusCode.NotFound:
                    return View("NotFound", error);

                case (int)HttpStatusCode.InternalServerError:
                    return View("InternalServerError", error);

                default:
                    return Error();
            }
        }
    }
}
