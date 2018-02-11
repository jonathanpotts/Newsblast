using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Discord;
using Discord.Rest;
using Newsblast.Web.Models;
using Newsblast.Web.Services;

namespace Newsblast.Web.Controllers
{
    [Route("guild")]
    [Authorize]
    public class GuildController : Controller
    {
        DiscordUserClient UserClient;
        DiscordBotClient BotClient;

        public GuildController(DiscordUserClient userClient, DiscordBotClient botClient)
        {
            UserClient = userClient;
            BotClient = botClient;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var guilds = new List<Guild>();

            var userClient = await UserClient.GetRestClientAsync();

            var discordGuilds = (await userClient.GetGuildSummariesAsync().First());

            foreach (var discordGuild in discordGuilds)
            {
                guilds.Add(new Guild()
                {
                    Id = discordGuild.Id,
                    Name = discordGuild.Name,
                    IconUrl = discordGuild.IconUrl,
                    IsAdministrator = discordGuild.Permissions.Administrator
                });
            }

            return View(guilds);
        }

        [Route("{id}")]
        public async Task<IActionResult> Inspect(ulong id)
        {
            var userClient = await UserClient.GetRestClientAsync();

            var userGuild = (await userClient.GetGuildSummariesAsync().First()).FirstOrDefault(e => e.Id == id);

            if (userGuild == null)
            {
                return NotFound();
            }

            var guild = new Guild()
            {
                Id = userGuild.Id,
                Name = userGuild.Name,
                IconUrl = userGuild.IconUrl,
                IsAdministrator = userGuild.Permissions.Administrator
            };

            if (guild.IsAdministrator)
            {
                var botClient = await BotClient.GetRestClientAsync();

                try
                {
                    var botGuild = await botClient.GetGuildAsync(id);

                    guild.BotConnected = true;

                    guild.Channels = new List<Channel>();

                    foreach (var channel in await botGuild.GetTextChannelsAsync())
                    {
                        guild.Channels.Add(new Channel()
                        {
                            Id = channel.Id,
                            Name = channel.Name,
                            Guild = guild
                        });
                    }
                }
                catch
                {
                    guild.BotConnected = false;
                }
            }
            else
            {
                guild.BotConnected = false;
            }

            return View(guild);
        }
    }
}
