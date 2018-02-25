using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Newsblast.Web.Controllers
{
    [Route("subscription")]
    [Authorize]
    public class SubscriptionController : Controller
    {
        [HttpPost]
        [Route("{id}/delete")]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            return View();
        }
    }
}