using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Discord.Rest;
using Newsblast.Shared.Data;
using Newsblast.Web.Models;
using Newsblast.Web.Models.ViewModels;
using Newsblast.Web.Services;

namespace Newsblast.Web.Controllers
{
    [Route("channel")]
    public class ChannelController : Controller
    {
        NewsblastContext Context;
        DiscordBotClient BotClient;
        DiscordUserClient UserClient;

        public ChannelController(NewsblastContext context, DiscordBotClient botClient, DiscordUserClient userClient)
        {
            Context = context;
            BotClient = botClient;
            UserClient = userClient;
        }

        [Route("{id}")]
        public async Task<IActionResult> Index(ulong id)
        {
            var botClient = await BotClient.GetRestClientAsync();

            try
            {
                var botChannel = await botClient.GetChannelAsync(id) as RestTextChannel;

                var userClient = await UserClient.GetRestClientAsync();
                var userGuild = (await userClient.GetGuildSummariesAsync().Single()).FirstOrDefault(e => e.Id == botChannel.GuildId);

                if (userGuild == null)
                {
                    return NotFound();
                }
                else if (!userGuild.Permissions.Administrator)
                {
                    return Unauthorized();
                }

                var botGuild = await botClient.GetGuildAsync(botChannel.GuildId);                

                var channel = new Channel()
                {
                    Id = id,
                    Name = botChannel.Name,
                    Guild = new Guild()
                    {
                        Id = botGuild.Id,
                        Name = botGuild.Name,
                        IconUrl = botGuild.IconUrl,
                        IsAdministrator = true,
                        BotConnected = true
                    }
                };

                channel.Subscriptions = await Context.Subscriptions
                    .Include(e => e.Source)
                    .Where(e => e.ChannelId == id)
                    .ToListAsync();

                var model = new ChannelViewModel()
                {
                    Channel = channel,
                    Sources = await Context.Sources.ToListAsync()
                };

                return View(model);
            }
            catch
            {
                return NotFound();
            }
        }
    }
}