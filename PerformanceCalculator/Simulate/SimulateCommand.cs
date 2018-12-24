// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using McMaster.Extensions.CommandLineUtils;
using PerformanceCalculator.Simulate.Mania;
using PerformanceCalculator.Simulate.Osu;
using PerformanceCalculator.Simulate.Taiko;

namespace PerformanceCalculator.Simulate
{
    [Command(Name = "performance", Description = "Computes the performance (pp) of a simulated play.")]
    [Subcommand("osu", typeof(OsuSimulateCommand))]
    [Subcommand("taiko", typeof(TaikoSimulateCommand))]
    [Subcommand("mania", typeof(ManiaSimulateCommand))]
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
