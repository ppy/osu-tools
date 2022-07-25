// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace PerformanceCalculator
{
    [Command(Name = "Login", Description = "Logs in to the osu! API.")]
    public class LoginCommand : APICommand
    {
        [UsedImplicitly]
        [Required]
        [Argument(98, Name = "client id", Description = "API Client ID, which you can get from here: https://osu.ppy.sh/home/account/edit#new-oauth-application")]
        public string ClientId { get; set; }

        [UsedImplicitly]
        [Required]
        [Argument(99, Name = "client secret", Description = "API Client Secret, which you can get from here: https://osu.ppy.sh/home/account/edit#new-oauth-application")]
        public string ClientSecret { get; set; }

        protected override bool RequiresLogin => false;

        public override void Execute()
        {
            Settings.SetValue(Configuration.Settings.ClientId, ClientId);
            Settings.SetValue(Configuration.Settings.ClientSecret, ClientSecret);

            Console.WriteLine(
                API.Login().Result
                    ? "Login success. You can now use other commands."
                    : "Login failed. Check the ID and secret and try again.");
        }
    }
}
