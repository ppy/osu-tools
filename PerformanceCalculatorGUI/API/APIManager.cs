
using System.Net.Http;
using System.Threading.Tasks;
using osu.Framework.Bindables;
using osu.Framework.IO.Network;
using osu.Game.Online;
using osu.Game.Online.API;

namespace PerformanceCalculatorGUI.API
{
    internal class APIManager
    {
        public static readonly EndpointConfiguration ENDPOINT_CONFIGURATION = new ProductionEndpointConfiguration();

        private readonly Bindable<string> clientIdBindable;
        private readonly Bindable<string> clientSecretBindable;

        private OAuthToken token;

        public APIManager(APIConfigManager configManager)
        {
            clientIdBindable = configManager.GetBindable<string>(APISettings.ClientId);
            clientSecretBindable = configManager.GetBindable<string>(APISettings.ClientSecret);
        }

        public async Task<T> GetJsonFromApi<T>(string request)
        {
            if (token == null)
                await getAccessToken();

            using var req = new JsonWebRequest<T>($"{ENDPOINT_CONFIGURATION.APIEndpointUrl}/api/v2/{request}");

            req.AddHeader(System.Net.HttpRequestHeader.Authorization.ToString(), $"Bearer {token.AccessToken}");
            await req.PerformAsync();

            return req.ResponseObject;
        }

        private async Task getAccessToken()
        {
            using var req = new JsonWebRequest<OAuthToken>($"{ENDPOINT_CONFIGURATION.APIEndpointUrl}/oauth/token")
            {
                Method = HttpMethod.Post
            };

            req.AddParameter("client_id", clientIdBindable.Value);
            req.AddParameter("client_secret", clientSecretBindable.Value);
            req.AddParameter("grant_type", "client_credentials");
            req.AddParameter("scope", "public");
            await req.PerformAsync();

            token = req.ResponseObject;
        }
    }
}
