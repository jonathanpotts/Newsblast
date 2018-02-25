using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Discord;

namespace Newsblast.Web.Controllers
{
    [Route("bot")]
    [Authorize]
    public class BotController : Controller
    {
        IConfiguration Configuration;

        public BotController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [Route("connect/{guildId}")]
        public IActionResult Connect(ulong guildId)
        {
            var clientId = Configuration["DiscordClientId"];
            var permissions = new GuildPermissions(readMessages: true, sendMessages: true, embedLinks: true, attachFiles: true).RawValue;
            var redirectUri = UrlEncoder.Default.Encode(Url.Action("Connected", "Bot", null, Request.Scheme));

            string discordUrl = $"https://discordapp.com/api/oauth2/authorize?response_type=code&client_id={clientId}&scope=bot&permissions={permissions}&guild_id={guildId}&redirect_uri={redirectUri}";
            return Redirect(discordUrl);
        }

        [Route("connected")]
        public IActionResult Connected(ulong guild_id)
        {
            return RedirectToAction("Inspect", "Guild", new { id = guild_id });
        }
    }
}