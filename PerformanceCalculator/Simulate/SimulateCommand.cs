// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using McMaster.Extensions.CommandLineUtils;
using PerformanceCalculator.Simulate.Osu;

namespace PerformanceCalculator.Simulate
{
    [Command(Name = "performance", Description = "Computes the performance (pp) of a simulated play.")]
    [Subcommand("osu", typeof(OsuSimulateCommand))]
    public class SimulateCommand : CommandBase
    {
        public int OnExecute(CommandLineApplication app, IConsole console)
        {
            console.WriteLine("You must specify a subcommand.");
            app.ShowHelp();
            return 1;
        }
    }
}
