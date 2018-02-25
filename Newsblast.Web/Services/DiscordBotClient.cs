using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Rest;

namespace Newsblast.Web.Services
{
    public class DiscordBotClient : IDisposable
    {
        DiscordRestClient RestClient = null;
        IConfiguration Configuration;

        public DiscordBotClient(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public async Task<DiscordRestClient> GetRestClientAsync()
        {
            if (RestClient == null)
            {
                RestClient = new DiscordRestClient();
            }

            if (RestClient.LoginState != LoginState.LoggedIn)
            {
                await RestClient.LoginAsync(TokenType.Bot, Configuration["DiscordBotToken"]);
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
