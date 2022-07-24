// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using osu.Framework.Bindables;
using osu.Framework.IO.Network;
using osu.Game.Online;
using osu.Game.Online.API;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI
{
    internal class APIManager
    {
        public static readonly EndpointConfiguration ENDPOINT_CONFIGURATION = new ProductionEndpointConfiguration();

        private readonly Bindable<string> clientIdBindable;
        private readonly Bindable<string> clientSecretBindable;

        private OAuthToken token;

        // WARN: keep in sync with /osu.Game/Online/API/APIAccess.cs APIVersion
        private const int api_version = 20220705;

        public APIManager(SettingsManager configManager)
        {
            clientIdBindable = configManager.GetBindable<string>(Settings.ClientId);
            clientSecretBindable = configManager.GetBindable<string>(Settings.ClientSecret);
        }

        public async Task<T> GetJsonFromApi<T>(string request)
        {
            if (token == null)
            {
                await getAccessToken();
                Debug.Assert(token != null);
            }

            using var req = new JsonWebRequest<T>($"{ENDPOINT_CONFIGURATION.APIEndpointUrl}/api/v2/{request}");
            req.AddHeader("x-api-version", api_version.ToString(CultureInfo.InvariantCulture));
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
