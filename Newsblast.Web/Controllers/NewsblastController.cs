using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Newsblast.Web.Controllers
{
    public abstract class NewsblastController : Controller
    {
        [NonAction]
        public async Task LogoutAsync()
        {
            var token = await HttpContext.GetTokenAsync("access_token");

            if (token != null)
            {
                using (var client = new HttpClient())
                {
                    await client.PostAsync("https://discordapp.com/api/oauth2/token/revoke", new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("token", token)
                    }));
                }
            }

            await HttpContext.SignOutAsync();
        }

        [NonAction]
        public IActionResult LogoutWithRedirect()
        {
            LogoutAsync().Wait();
            return RedirectToAction("Relay", "Home", new { url = Request.Path + Request.QueryString });
        }
    }
}