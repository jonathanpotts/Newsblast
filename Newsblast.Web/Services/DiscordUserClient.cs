using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Discord;
using Discord.Rest;

namespace Newsblast.Web.Services
{
    public class DiscordUserClient : IDisposable
    {
        DiscordRestClient RestClient = null;
        IActionContextAccessor ActionContextAccessor;

        public DiscordUserClient(IActionContextAccessor actionContextAccessor)
        {
            ActionContextAccessor = actionContextAccessor;
        }

        public async Task<DiscordRestClient> GetRestClientAsync()
        {
            var actionContext = ActionContextAccessor.ActionContext;
            var context = actionContext.HttpContext;

            if (context.User.Identity.IsAuthenticated)
            {
                if (RestClient == null)
                {
                    RestClient = new DiscordRestClient();
                }

                if (RestClient.LoginState != LoginState.LoggedIn)
                {
                    var token = await context.GetTokenAsync("access_token");

                    try
                    {
                        await RestClient.LoginAsync(TokenType.Bearer, token);
                    }
                    catch (Exception)
                    {
                        RestClient = null;
                        await context.SignOutAsync();
                    }
                }
            }

            return RestClient;
        }

        public void Dispose()
        {
            if (RestClient != null)
            {
                RestClient.Dispose();
            }

            RestClient = null;
        }
    }
}
