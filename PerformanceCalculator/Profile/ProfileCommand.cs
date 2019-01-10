// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace PerformanceCalculator.Profile
{
    [Command(Name = "profile", Description = "Computes the total performance (pp) of a profile.")]
    public class ProfileCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required]
        [Argument(0, Name = "profile name", Description = "Required. Username of the osu account to be checked (not user id)")]
        public string ProfileName { get; }

        [UsedImplicitly]
        [Required]
        [Argument(1, Name = "api key", Description = "Required. API Key, which you can get from here: https://osu.ppy.sh/p/api")]
        public string Key { get; }

        [UsedImplicitly]
        [Option(Template = "-r|--ruleset:<ruleset-id>", Description = "Optional. The ruleset to compute the profile for; only osu and taiko are currently implemented. \n"
                                                                     + "Values: 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania. It is 0 by default.")]
        [AllowedValues("0", "1", "2", "3")]
        public int? Ruleset { get; }

        [UsedImplicitly]
        [Option(Template = "-c|--cache:<caching-path>", Description = "Optional. Caches files in the specified directory. Also use if you are trying to use previously cached files in that directory. Useful for running the same profile multiple times.")]
        public string CachePath { get; }

        [UsedImplicitly]
        [Option(Template = "-b|--bonus", Description = "Optional. Include the argument if (approxiamate) Bonus PP should be included.")]
        public bool Bonus { get; }

        protected override IProcessor CreateProcessor() => new ProfileProcessor(this);
    }
}
