﻿using System.Net;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Discord;
using Newsblast.Web.Services;

namespace Newsblast.Web.Controllers
{
    [Route("bot")]
    [Authorize]
    public class BotController : NewsblastController
    {
        IConfiguration Configuration;
        DiscordUserClient UserClient;

        public BotController(IConfiguration configuration, DiscordUserClient userClient)
        {
            Configuration = configuration;
            UserClient = userClient;
        }

        [Route("connect/{guildId}")]
        public async Task<IActionResult> Connect(ulong guildId)
        {
            var userClient = await UserClient.GetRestClientAsync();

            if (userClient == null)
            {
                return LogoutWithRedirect();
            }

            var clientId = Configuration["DiscordClientId"];
            var permissions = new GuildPermissions(viewChannel: true, sendMessages: true, embedLinks: true, attachFiles: true).RawValue;
            var redirectUri = UrlEncoder.Default.Encode(Url.Action("Connected", "Bot", null, Request.Scheme));

            string discordUrl = $"https://discordapp.com/api/oauth2/authorize?response_type=code&client_id={clientId}&scope=bot&permissions={permissions}&guild_id={guildId}&redirect_uri={redirectUri}";
            return Redirect(discordUrl);
        }

        [Route("connected")]
        public IActionResult Connected(ulong guild_id = 0)
        {
            if (guild_id == 0)
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            return RedirectToAction("Inspect", "Guild", new { id = guild_id });
        }
    }
}