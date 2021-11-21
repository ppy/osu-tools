// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Framework.IO.Network;

namespace PerformanceCalculator
{
    public abstract class ApiCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required]
        [Argument(98, Name = "client id", Description = "API Client ID, which you can get from here: https://osu.ppy.sh/home/account/edit#new-oauth-application")]
        public string ClientId { get; }

        [UsedImplicitly]
        [Required]
        [Argument(99, Name = "client secret", Description = "API Client Secret, which you can get from here: https://osu.ppy.sh/home/account/edit#new-oauth-application")]
        public string ClientSecret { get; }

        private string apiAccessToken;

        public override void OnExecute(CommandLineApplication app, IConsole console)
        {
            getAccessToken();
            base.OnExecute(app, console);
        }

        protected dynamic GetJsonFromApi(string request)
        {
            using var req = new JsonWebRequest<dynamic>($"{Program.ENDPOINT_CONFIGURATION.APIEndpointUrl}/api/v2/{request}");

            req.AddHeader(System.Net.HttpRequestHeader.Authorization.ToString(), $"Bearer {apiAccessToken}");
            req.Perform();

            return req.ResponseObject;
        }

        private void getAccessToken()
        {
            using var req = new JsonWebRequest<dynamic>($"{Program.ENDPOINT_CONFIGURATION.APIEndpointUrl}/oauth/token")
            {
                Method = HttpMethod.Post
            };

            req.AddParameter("client_id", ClientId);
            req.AddParameter("client_secret", ClientSecret);
            req.AddParameter("grant_type", "client_credentials");
            req.AddParameter("scope", "public");
            req.Perform();

            apiAccessToken = req.ResponseObject.access_token.ToString();
        }
    }
}
