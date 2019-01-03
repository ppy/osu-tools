// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mania;

namespace PerformanceCalculator.Simulate.Mania
{
    [Command(Name = "simulate mania", Description = "Computes the performance (pp) of a simulated osu!mania play.")]
    public class ManiaSimulateCommand : BaseSimulateCommand
    {
        [UsedImplicitly]
        [Required, FileExists]
        [Argument(0, Name = "beatmap", Description = "Required. The beatmap file (.osu).")]
        public override string Beatmap { get; }

        [UsedImplicitly]
        [Option(Template = "-s|--score <score>", Description = "Score. An integer 0-1000000.")]
        public override int Score { get; } = 1000000;

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-m|--mod <mod>", Description = "One for each mod. The mods to compute the performance with."
                                                                                            + " Values: hr, dt, fl, 4k, 5k, etc...")]
        public override string[] Mods { get; }

        public override Ruleset Ruleset => new ManiaRuleset();

        protected override IProcessor CreateProcessor() => new ManiaSimulateProcessor(this);
    }
}
