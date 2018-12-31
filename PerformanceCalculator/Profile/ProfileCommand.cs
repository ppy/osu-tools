// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace PerformanceCalculator.Profile
{
    [Command(Name = "profile", Description = "Returns the total pp of a profile.")]
    public class ProfileCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required]
        [Argument(0, Name = "profile name", Description = "Required. Name of the osu account (not user id)")]
        public string ProfileName { get; }

        [UsedImplicitly]
        [Required]
        [Argument(1, Name = "api key", Description = "Required. API Key, which you can get from here: https://osu.ppy.sh/p/api")]
        public string Key { get; }

        [UsedImplicitly]
        [Required,FileExists]
        [Argument(2, Name = "path", Description = "Required. Path to a txt file that is currently made. Must end with .txt")]
        public string Path { get; }

        [UsedImplicitly]
        [Option(Template = "-b|--bonus <number>", Description = "Whether or not Bonus PP should be included. 1 is included, 0 is not included. Default is 0.")]
        public int? Bonus { get; }

        protected override IProcessor CreateProcessor() => new ProfileProcessor(this);
    }
}
