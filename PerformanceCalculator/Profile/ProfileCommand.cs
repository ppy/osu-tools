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
        [Required]
        [Argument(2, Name = "path", Description = "Required. Path to an open directory. Will create a txt file in that directory called ProfileCalculator.txt that will take up a few KB.")]
        public string Path { get; }

        [UsedImplicitly]
        [Option(Template = "-b|--bonus <number>", Description = "Optional. Whether or not Bonus PP should be included. 1 is included, 0 is not included. Default is 0.")]
        public int? Bonus { get; }

        protected override IProcessor CreateProcessor() => new ProfileProcessor(this);
    }
}
