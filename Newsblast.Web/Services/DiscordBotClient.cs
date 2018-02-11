using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Rest;

namespace Newsblast.Web.Services
{
    public class DiscordBotClient
    {
        DiscordRestClient RestClient = new DiscordRestClient();
        IConfiguration Configuration;

        public DiscordBotClient(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public async Task<DiscordRestClient> GetRestClientAsync()
        {
            if (RestClient.LoginState != LoginState.LoggedIn)
            {
                await RestClient.LoginAsync(TokenType.Bot, Configuration["DiscordBotToken"]);
            }

            return RestClient;
        }
    }
}
