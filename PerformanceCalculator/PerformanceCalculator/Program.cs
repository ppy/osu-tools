// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Framework.Platform;
using osu.Game;

namespace PerformanceCalculator
{
    public class Program
    {
        public static void Main(string[] args)
            => CommandLineApplication.Execute<Program>(args);

        [UsedImplicitly]
        [Required, FileExists]
        [Option(Template = "-b|--beatmap", Description = "Required. The beatmap for the replays.")]
        public string Beatmap { get; }

        [UsedImplicitly]
        [Required, FileExists]
        [Option(Template = "-r|--replay", Description = "One or more replays to calculate the performance for.")]
        public string[] Replays { get; }

        [UsedImplicitly]
        public void OnExecute(CommandLineApplication app, IConsole console)
        {
            using (var host = new HeadlessGameHost("performance"))
            {
                var game = new OsuGameBase();
                game.OnLoadComplete += _ => game.Add(new Calculator(Beatmap, Replays, console));

                host.Run(game);
            }
        }
    }
}
