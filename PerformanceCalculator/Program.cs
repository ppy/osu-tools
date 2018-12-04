// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using McMaster.Extensions.CommandLineUtils;
using osu.Framework.Logging;
using osu.Game.Beatmaps.Formats;
using PerformanceCalculator.Difficulty;
using PerformanceCalculator.Performance;

namespace PerformanceCalculator
{
    [Command("dotnet PerformanceCalculator.dll")]
    [Subcommand("difficulty", typeof(DifficultyCommand))]
    [Subcommand("performance", typeof(PerformanceCommand))]
    public class Program : CommandBase
    {
        public static void Main(string[] args)
        {
            LegacyDifficultyCalculatorBeatmapDecoder.Register();

            Logger.Enabled = false;
            CommandLineApplication.Execute<Program>(args);
        }

        public int OnExecute(CommandLineApplication app, IConsole console)
        {
            console.WriteLine("You must specify a subcommand.");
            app.ShowHelp();
            return 1;
        }
    }
}
