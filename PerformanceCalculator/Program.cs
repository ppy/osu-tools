﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using osu.Framework.Logging;
using osu.Game.Beatmaps.Formats;
using PerformanceCalculator.Difficulty;
using PerformanceCalculator.Leaderboard;
using PerformanceCalculator.Performance;
using PerformanceCalculator.Profile;
using PerformanceCalculator.Simulate;

namespace PerformanceCalculator
{
    [Command("dotnet PerformanceCalculator.dll")]
    [Subcommand(typeof(DifficultyCommand))]
    [Subcommand(typeof(ModsCommand))]
    [Subcommand(typeof(PerformanceCommand))]
    [Subcommand(typeof(ProfileCommand))]
    [Subcommand(typeof(SimulateListingCommand))]
    [Subcommand(typeof(LeaderboardCommand))]
    [HelpOption("-?|-h|--help")]
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Logger.Enabled = false;

            LegacyDifficultyCalculatorBeatmapDecoder.Register();

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
