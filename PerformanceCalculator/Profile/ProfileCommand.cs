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

        [Option(Template = "-b|--bonus", Description = "Optional. Include the argument if (approxiamate) Bonus PP should be included.")]
        public bool Bonus { get; }

        protected override IProcessor CreateProcessor() => new ProfileProcessor(this);
    }
}
