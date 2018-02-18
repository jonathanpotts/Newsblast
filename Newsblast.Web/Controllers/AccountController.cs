using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Newsblast.Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        [Route("logout")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            if (User.Identity.IsAuthenticated)
            {
                var token = User.Claims.Where(e => e.Type == "urn:discord:token").FirstOrDefault()?.Value;

                if (token != null)
                {
                    var client = new HttpClient();
                    await client.PostAsync("https://discordapp.com/api/oauth2/token/revoke", new FormUrlEncodedContent(new []
                    {
                        new KeyValuePair<string, string>("token", token)
                    }));
                }
            }

            await HttpContext.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }
    }
}