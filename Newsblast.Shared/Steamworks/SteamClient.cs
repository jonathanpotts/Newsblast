using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Newsblast.Shared.Steamworks
{
    internal static class SteamClient
    {
        const string ApiUrl = "https://api.steampowered.com";

        internal static async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string endpoint, IReadOnlyDictionary<string, string> data)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(ApiUrl, UriKind.Absolute);

                using (var encodedData = new FormUrlEncodedContent(data))
                {
                    if (method == HttpMethod.Get)
                    {
                        var queryString = "?" + await encodedData.ReadAsStringAsync();

                        return await httpClient.GetAsync(new Uri(endpoint + queryString, UriKind.Relative));
                    }
                    else if (method == HttpMethod.Post)
                    {
                        return await httpClient.PostAsync(new Uri(endpoint, UriKind.Relative), encodedData);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }
    }
}
