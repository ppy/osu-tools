// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace PerformanceCalculator.Simulate.Mania
{
    [Command(Name = "simulate mania", Description = "Computes the performance (pp) of a simulated mania play.")]
    public class ManiaSimulateCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required, FileExists]
        [Argument(0, Name = "beatmap", Description = "Required. The beatmap file (.osu).")]
        public string Beatmap { get; }

        [UsedImplicitly]
        [Option(Template = "-s|--score <score>", Description = "Score. An integer 0-1000000.")]
        public int Score { get; }

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-m|--mod <mod>", Description = "One for each mod. The mods to compute the performance with."
                                                                                            + "Values: hr, dt, fl, 4k, 5k, etc...")]
        public string[] Mods { get; }

        protected override IProcessor CreateProcessor() => new ManiaSimulateProcessor(this);
    }
}
