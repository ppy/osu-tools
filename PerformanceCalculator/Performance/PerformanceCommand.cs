// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace PerformanceCalculator.Performance
{
    [Command(Name = "performance", Description = "Computes the performance (pp) of replays on a beatmap.")]
    public class PerformanceCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required, FileExists]
        [Argument(0, Name = "beatmap", Description = "Required. The beatmap file (.osu) corresponding to the replays.")]
        public string Beatmap { get; }

        [UsedImplicitly]
        [FileExists]
        [Option(Template = "-r|--replay <file>", Description = "One for each replay. The replay file.")]
        public string[] Replays { get; }

        protected override IProcessor CreateProcessor() => new PerformanceProcessor(this);
    }
}
