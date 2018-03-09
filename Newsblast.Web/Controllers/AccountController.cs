using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Newsblast.Web.Controllers
{
    [Authorize]
    public class AccountController : NewsblastController
    {
        [Route("logout")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            return LogoutWithRedirect();
        }
    }
}