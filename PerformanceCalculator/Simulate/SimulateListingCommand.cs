// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace PerformanceCalculator.Simulate
{
    [Command(Name = "performance", Description = "Computes the performance (pp) of a simulated play.")]
    [Subcommand("osu", typeof(OsuSimulateCommand))]
    [Subcommand("taiko", typeof(TaikoSimulateCommand))]
    [Subcommand("mania", typeof(ManiaSimulateCommand))]
    public class SimulateListing
    {
        [UsedImplicitly]
        public int OnExecute(CommandLineApplication app, IConsole console)
        {
            console.WriteLine("You must specify a subcommand.");
            app.ShowHelp();
            return 1;
        }
    }
}
