// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using McMaster.Extensions.CommandLineUtils;
using PerformanceCalculator.Difficulty;
using PerformanceCalculator.Performance;

namespace PerformanceCalculator
{
    [Command("main")]
    [Subcommand("difficulty", typeof(DifficultyCommand))]
    [Subcommand("performance", typeof(PerformanceCommand))]
    public class Program : CommandBase
    {
        public static void Main(string[] args)
            => CommandLineApplication.Execute<Program>(args);
        
        public int OnExecute(CommandLineApplication app, IConsole console)
        {
            console.WriteLine("You must specify a subcommand.");
            app.ShowHelp();
            return 1;
        }
    }
}
