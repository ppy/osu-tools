// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace PerformanceCalculator.Simulate
{
    [Command(Name = "performance", Description = "Computes the performance (pp) of a simulated play. Only works on osu standard for now.")]
    public class SimulateCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required, FileExists]
        [Argument(0, Name = "beatmap", Description = "Required. The beatmap file (.osu).")]
        public string Beatmap { get; }

        [UsedImplicitly]
        [Option(Template = "-a|--accuracy <accuracy>", Description = "Accuracy. Enter as decimal 0-100. Defaults to 100. Scales number of 300s and 100s as well.")]
        public double? Accuracy { get; }

        [UsedImplicitly]
        [Option(Template = "-c|--combo|--max-combo <combo>", Description = "Max Combo. Enter as integer. Defaults to beatmap maximum.")]
        public int? MaxCombo { get; }

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-m|--mod <mod>", Description = "One for each mod. The mods to compute the performance with."
                                                                                            + "Values: hr, dt, hd, fl, ez, etc...")]
        public string[] Mods { get; }

        [UsedImplicitly]
        [Option(Template = "-M|--misses <misses>", Description = "Number of misses. Enter as integer.")]
        public int? Misses { get; }

        protected override IProcessor CreateProcessor() => new SimulateProcessor(this);
    }
}
