using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Discord;
using Discord.Rest;

namespace Newsblast.Web.Services
{
    public class DiscordUserClient : IDisposable
    {
        DiscordRestClient RestClient = null;
        IHttpContextAccessor HttpContextAccessor;

        public DiscordUserClient(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
        }

        public async Task<DiscordRestClient> GetRestClientAsync()
        {
            if (HttpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
            {
                var token = await HttpContextAccessor.HttpContext.GetTokenAsync("access_token");

                if (RestClient == null)
                {
                    RestClient = new DiscordRestClient();
                }

                if (RestClient.LoginState != LoginState.LoggedIn)
                {
                    await RestClient.LoginAsync(TokenType.Bearer, token);
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
