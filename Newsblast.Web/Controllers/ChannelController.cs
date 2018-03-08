using System;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
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
            try
            {
                var channel = await GetChannelAsync(id);

                if (channel == null)
                {
                    return NotFound();
                }

                var model = new ChannelViewModel()
                {
                    Channel = channel,
                    Sources = await Context.Sources.ToListAsync()
                };

                return View(model);
            }
            catch (AuthenticationException)
            {
                return Unauthorized();
            }
        }

        [HttpGet]
        [Route("{id}/subscribe")]
        public async Task<IActionResult> Subscribe(ulong id)
        {
            try
            {
                var channel = await GetChannelAsync(id);

                if (channel == null)
                {
                    return NotFound();
                }

                var model = new SubscribeViewModel()
                {
                    Channel = channel,
                    Sources = await Context.Sources.ToListAsync()
                };

                return View(model);
            }
            catch (AuthenticationException)
            {
                return Unauthorized();
            }
        }

        [HttpPost]
        [Route("{id}/subscribe")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(ulong id, SubscribeViewModel model)
        {
            try
            {
                var channel = await GetChannelAsync(id);

                if (channel == null)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    var source = Context.Sources.FirstOrDefault(e => e.Id == model.SourceId);
                    if (source == null)
                    {
                        ModelState.AddModelError("SourceId", "The specified feed does not exist.");

                        model.Channel = channel;
                        model.Sources = await Context.Sources.ToListAsync();
                        return View(model);
                    }

                    if (channel.Subscriptions.FirstOrDefault(e => e.SourceId == model.SourceId) != null)
                    {
                        ModelState.AddModelError("SourceId", "The specified feed has already been subscribed to by this channel.");

                        model.Channel = channel;
                        model.Sources = await Context.Sources.ToListAsync();
                        return View(model);
                    }

                    await Context.Subscriptions.AddAsync(new Shared.Data.Models.Subscription()
                    {
                        ChannelId = id,
                        SourceId = model.SourceId
                    });

                    await Context.SaveChangesAsync();

                    try
                    {
                        var client = await BotClient.GetRestClientAsync();
                        var discordChannel = await client.GetChannelAsync(id) as RestTextChannel;
                        await discordChannel.SendMessageAsync($"This channel has been subscribed to **{source.Name}**.");
                    }
                    catch (Exception)
                    {
                    }

                    return RedirectToAction("Index", new { id });
                }

                model.Channel = channel;
                model.Sources = await Context.Sources.ToListAsync();
                return View(model);
            }
            catch (AuthenticationException)
            {
                return Unauthorized();
            }
        }

        [HttpPost]
        [Route("unsubscribe/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unsubscribe(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subscription = Context.Subscriptions.Include(e => e.Source).FirstOrDefault(e => e.Id == id);

            if (subscription == null)
            {
                return NotFound();
            }

            try
            {
                var channelId = subscription.ChannelId;

                var channel = await GetChannelAsync(channelId);

                if (channel == null)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    Context.Subscriptions.Remove(subscription);
                    await Context.SaveChangesAsync();

                    try
                    {
                        var client = await BotClient.GetRestClientAsync();
                        var discordChannel = await client.GetChannelAsync(subscription.ChannelId) as RestTextChannel;
                        await discordChannel.SendMessageAsync($"This channel has been unsubscribed from **{subscription.Source.Name}**.");
                    }
                    catch (Exception)
                    {
                    }

                    return RedirectToAction("Index", new { id = channelId });
                }

                return BadRequest();
            }
            catch (AuthenticationException)
            {
                return Unauthorized();
            }
        }

        [NonAction]
        public async Task<Channel> GetChannelAsync(ulong id)
        {
            if (UserClient == null)
            {
                Challenge();
            }

            var botClient = await BotClient.GetRestClientAsync();

            try
            {
                var botChannel = await botClient.GetChannelAsync(id) as RestTextChannel;

                var userClient = await UserClient.GetRestClientAsync();
                var userGuild = (await userClient.GetGuildSummariesAsync().Single()).FirstOrDefault(e => e.Id == botChannel.GuildId);

                if (userGuild == null)
                {
                    return null;
                }
                else if (!userGuild.Permissions.Administrator)
                {
                    throw new AuthenticationException();
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

                channel.Subscriptions = await Context.Subscriptions.Where(e => e.ChannelId == id).ToListAsync();

                return channel;
            }
            catch (AuthenticationException ex)
            {
                throw ex;
            }
            catch
            {
                return null;
            }
        }
    }
}