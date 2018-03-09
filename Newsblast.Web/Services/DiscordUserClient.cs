using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Rest;

namespace Newsblast.Web.Services
{
    public class DiscordUserClient : IDisposable
    {
        DiscordRestClient RestClient = null;
        IHttpContextAccessor HttpContextAccessor;
        IConfiguration Configuration;

        public DiscordUserClient(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            HttpContextAccessor = httpContextAccessor;
            Configuration = configuration;
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

        public async Task<bool> IsAdministratorAsync()
        {
            if (RestClient == null)
            {
                await GetRestClientAsync();

                if (RestClient == null)
                {
                    return false;
                }
            }

            var adminIds = Configuration["AdministratorIds"]?.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();

            if (adminIds == null)
            {
                return false;
            }

            return adminIds.Contains(RestClient.CurrentUser.Id.ToString());
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
