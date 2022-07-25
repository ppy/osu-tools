// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using McMaster.Extensions.CommandLineUtils;
using osu.Framework;
using osu.Framework.Platform;
using PerformanceCalculator.Configuration;

namespace PerformanceCalculator
{
    public abstract class APICommand : ProcessorCommand
    {
        /// <summary>
        /// Whether this command needs to be logged in to the API to run.
        /// </summary>
        protected virtual bool RequiresLogin => true;

        protected SettingsManager Settings { get; private set; }
        protected APIManager API { get; private set; }

        public sealed override void OnExecute(CommandLineApplication app, IConsole console)
        {
            using (DesktopGameHost host = Host.GetSuitableDesktopHost("PerformanceCalculator", new HostOptions { PortableInstallation = true }))
            {
                Settings = new SettingsManager(host.GetStorage("."));
                API = new APIManager(Settings);

                if (RequiresLogin)
                {
                    if (!API.Login().Result)
                    {
                        System.Console.WriteLine("Log in required. Run with 'login <client_id> <client_secret>' to login.");
                        return;
                    }
                }

                base.OnExecute(app, console);
            }
        }
    }
}
