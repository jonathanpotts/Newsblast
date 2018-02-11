using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Discord;
using Discord.Rest;

namespace Newsblast.Web.Services
{
    public class DiscordUserClient
    {
        DiscordRestClient RestClient = new DiscordRestClient();
        IHttpContextAccessor HttpContextAccessor;

        public DiscordUserClient(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
        }

        public async Task<DiscordRestClient> GetRestClientAsync()
        {
            if (RestClient.LoginState != LoginState.LoggedIn)
            {
                var token = HttpContextAccessor.HttpContext.User.Claims.FirstOrDefault(e => e.Type == "urn:discord:token")?.Value;
                await RestClient.LoginAsync(TokenType.Bearer, token);
            }

            return RestClient;
        }
    }
}
