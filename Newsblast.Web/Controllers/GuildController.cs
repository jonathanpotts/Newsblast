using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Discord;
using Discord.Rest;
using Newsblast.Web.Models;

namespace Newsblast.Web.Controllers
{
    [Authorize]
    public class GuildController : Controller
    {
        [Route("/guild")]
        public async Task<IActionResult> Index()
        {
            var token = User.Claims.Where(e => e.Type == "urn:discord:token").Single().Value;

            var discord = new DiscordRestClient();
            await discord.LoginAsync(TokenType.Bearer, token);

            var guilds = new List<Guild>();

            var discordGuilds = (await discord.GetGuildSummariesAsync().Single()).Where(e => e.IsOwner == true);

            foreach (var discordGuild in discordGuilds)
            {
                guilds.Add(new Guild()
                {
                    Id = discordGuild.Id,
                    Name = discordGuild.Name,
                    IconUrl = discordGuild.IconUrl
                });
            }

            return View(guilds);
        }

        [Route("/guild/{id}")]
        public async Task<IActionResult> Inspect(ulong id)
        {
            var token = User.Claims.Where(e => e.Type == "urn:discord:token").Single().Value;

            var discord = new DiscordRestClient();
            await discord.LoginAsync(TokenType.Bearer, token);

            var discordGuild = (await discord.GetGuildSummariesAsync().Single()).Where(e => e.Id == id).SingleOrDefault();

            if (discordGuild == null)
            {
                return NotFound();
            }

            if (!discordGuild.IsOwner)
            {
                return Unauthorized();
            }

            var guild = new Guild()
            {
                Id = discordGuild.Id,
                Name = discordGuild.Name,
                IconUrl = discordGuild.IconUrl
            };

            return View(guild);
        }
    }
}
