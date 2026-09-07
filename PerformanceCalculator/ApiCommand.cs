// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net.Http;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Framework.IO.Network;
using osu.Game.Online.API;

namespace PerformanceCalculator
{
    public abstract class ApiCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required]
        [Argument(98, Name = "client id", Description = "API Client ID, which you can get from here: https://osu.ppy.sh/home/account/edit#new-oauth-application")]
        public string ClientId { get; } = null!;

        [UsedImplicitly]
        [Required]
        [Argument(99, Name = "client secret", Description = "API Client Secret, which you can get from here: https://osu.ppy.sh/home/account/edit#new-oauth-application")]
        public string ClientSecret { get; } = null!;

        private string? apiAccessToken;

        public override void OnExecute(CommandLineApplication app, IConsole console)
        {
            getAccessToken();
            base.OnExecute(app, console);
        }

        protected T GetJsonFromApi<T>(string request, HttpMethod? method = null, Dictionary<string, string>? parameters = null)
        {
            var now = DateTimeOffset.Now;
            int apiVersion = now.Year * 10000 + now.Month * 100 + now.Day;

            using var req = new JsonWebRequest<T>($"{Program.ENDPOINT_CONFIGURATION.APIUrl}/api/v2/{request}");
            req.Method = method ?? HttpMethod.Get;
            req.AddHeader("x-api-version", apiVersion.ToString(CultureInfo.InvariantCulture));
            req.AddHeader(nameof(System.Net.HttpRequestHeader.Authorization), $"Bearer {apiAccessToken}");

            if (parameters != null)
            {
                foreach ((string key, string value) in parameters)
                    req.AddParameter(key, value);
            }

            req.Perform();

            return req.ResponseObject;
        }

        private void getAccessToken()
        {
            using var req = new JsonWebRequest<OAuthToken>($"{Program.ENDPOINT_CONFIGURATION.APIUrl}/oauth/token")
            {
                Method = HttpMethod.Post
            };

            req.AddParameter("client_id", ClientId);
            req.AddParameter("client_secret", ClientSecret);
            req.AddParameter("grant_type", "client_credentials");
            req.AddParameter("scope", "public");
            req.Perform();

            apiAccessToken = req.ResponseObject.AccessToken;
        }
    }
}
