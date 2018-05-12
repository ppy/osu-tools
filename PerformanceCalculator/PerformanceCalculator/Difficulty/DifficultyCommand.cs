// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace PerformanceCalculator.Difficulty
{
    [Command(Name = "difficulty", Description = "Computes the difficulty of a beatmap.")]
    public class DifficultyCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required, FileExists]
        [Argument(0, Name = "beatmap", Description = "Required. The beatmap (.osu) to compute the difficulty for.")]
        public string Beatmap { get; }
        
        [UsedImplicitly]
        [Option(CommandOptionType.SingleOrNoValue, Template = "-r|--ruleset:<ruleset-id>", Description = "Optional. The ruleset to compute the beatmap difficulty for, if it's a convertible beatmap.\n"
                                                                                                         + "Values: 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania")]
        [AllowedValues("0", "1", "2", "3")]
        public int? Ruleset { get; }
        
        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-m|--m <mod>", Description = "One for each mod. The mods to compute the difficulty with."
                                                                                          + "Values: hr, dt, hd, fl, ez, 4k, 5k, etc...")]
        public string[] Mods { get; }

        protected override Processor CreateProcessor() => new DifficultyProcessor(this);
    }
}
