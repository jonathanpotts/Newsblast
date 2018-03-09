using System;
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
            var token = await HttpContextAccessor.HttpContext.GetTokenAsync("access_token");

            if (token != null)
            {
                if (RestClient == null)
                {
                    RestClient = new DiscordRestClient();
                }

                if (RestClient.LoginState != LoginState.LoggedIn)
                {
                    try
                    {
                        await RestClient.LoginAsync(TokenType.Bearer, token);
                    }
                    catch (Exception)
                    {
                        RestClient = null;
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
