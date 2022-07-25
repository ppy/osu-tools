// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Framework;
using osu.Framework.Platform;
using PerformanceCalculator.Configuration;

namespace PerformanceCalculator
{
    public abstract class APICommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required]
        [Argument(98, Name = "client id", Description = "API Client ID, which you can get from here: https://osu.ppy.sh/home/account/edit#new-oauth-application")]
        public string ClientId { get; }

        [UsedImplicitly]
        [Required]
        [Argument(99, Name = "client secret", Description = "API Client Secret, which you can get from here: https://osu.ppy.sh/home/account/edit#new-oauth-application")]
        public string ClientSecret { get; }

        protected APIManager API;

        public override void OnExecute(CommandLineApplication app, IConsole console)
        {
            using (DesktopGameHost host = Host.GetSuitableDesktopHost("PerformanceCalculatorGUI", new HostOptions { PortableInstallation = true }))
                API = new APIManager(new SettingsManager(host.Storage));

            base.OnExecute(app, console);
        }
    }
}
