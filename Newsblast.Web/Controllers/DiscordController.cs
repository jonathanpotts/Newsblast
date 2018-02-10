using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Discord;
using Discord.Rest;

namespace Newsblast.Web.Controllers
{
    [Authorize]
    public class DiscordController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var token = User.Claims.Where(e => e.Type == "urn:discord:token").Single().Value;

            var discord = new DiscordRestClient();
            await discord.LoginAsync(TokenType.Bearer, token);

            var guilds = await discord.GetGuildsAsync();

            return View();
        }
    }
}
