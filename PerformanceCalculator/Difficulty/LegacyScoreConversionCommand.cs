// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Database;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using Realms;

namespace PerformanceCalculator.Difficulty
{
    [Command(Name = "legacy-score-conversion", Description = "Performs score conversion for a stable score with the given parameters.")]
    [HelpOption("-?|-h|--help")]
    public class LegacyScoreConversionCommand
    {
        [UsedImplicitly]
        [Required]
        [Argument(0, Name = "beatmap", Description = "Required. Can be either a path to beatmap file (.osu) or beatmap ID.")]
        public string Beatmap { get; }

        [UsedImplicitly]
        [Required]
        [Option(CommandOptionType.SingleValue, Template = "-r|--ruleset:<ruleset-id>", Description = "The ruleset to perform score total conversion in.\n"
                                                                                                     + "Values: 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania")]
        [AllowedValues("0", "1", "2", "3")]
        public int Ruleset { get; }

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-m|--m <mod>", Description = "One for each mod. The mods to compute the difficulty with."
                                                                                          + "Values: hr, dt, hd, fl, ez, 4k, 5k, etc...")]
        public string[] Mods { get; }

        [Option(CommandOptionType.SingleValue, Template = "-T|--greats", Description = "Number of greats.")]
        public int Greats { get; set; }

        [Option(CommandOptionType.SingleValue, Template = "-D|--goods", Description = "Number of goods.")]
        public int Goods { get; set; }

        [Option(CommandOptionType.SingleValue, Template = "-M|--mehs", Description = "Number of mehs.")]
        public int Mehs { get; set; }

        [Option(CommandOptionType.SingleValue, Template = "-X|--misses", Description = "Number of misses.")]
        public int Misses { get; set; }

        [Option(CommandOptionType.SingleValue, Template = "-G|--geki", Description = "Number of gekis.")]
        public int Gekis { get; set; }

        [Option(CommandOptionType.SingleValue, Template = "-K|--katu", Description = "Number of katus.")]
        public int Katus { get; set; }

        [Option(CommandOptionType.SingleValue, Template = "-c|--max-combo", Description = "Max combo achieved by user.")]
        public int MaxCombo { get; set; }

        [Option(CommandOptionType.SingleValue, Template = "-s|--score", Description = "Total score achieved by user.")]
        public int TotalScore { get; set; }

        [UsedImplicitly]
        public virtual void OnExecute(CommandLineApplication app, IConsole console)
        {
            var ruleset = LegacyHelper.GetRulesetFromLegacyID(Ruleset);

            var workingBeatmap = ProcessorWorkingBeatmap.FromFileOrId(Beatmap);
            // bit of a hack to discard non-legacy mods.
            var mods = ruleset.ConvertFromLegacyMods(ruleset.ConvertToLegacyMods(getMods(ruleset)))
                              .Append(ruleset.CreateMod<ModClassic>())
                              .ToArray();
            var beatmap = workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo, mods);

            var scoreInfo = new ScoreInfo(beatmap.BeatmapInfo, ruleset.RulesetInfo)
            {
                IsLegacyScore = true,
                LegacyTotalScore = TotalScore,
                MaxCombo = MaxCombo,
                Mods = mods,
            };
            scoreInfo.SetCount300(Greats);
            scoreInfo.SetCount100(Goods);
            scoreInfo.SetCount50(Mehs);
            scoreInfo.SetCountGeki(Gekis);
            scoreInfo.SetCountKatu(Katus);
            scoreInfo.SetCountMiss(Misses);

            LegacyScoreDecoder.PopulateMaximumStatistics(scoreInfo, workingBeatmap);
            StandardisedScoreMigrationTools.UpdateFromLegacy(scoreInfo, workingBeatmap);
            console.WriteLine($"Converted total score: {scoreInfo.TotalScore}");
        }

        private Mod[] getMods(Ruleset ruleset)
        {
            if (Mods == null)
                return Array.Empty<Mod>();

            var availableMods = ruleset.CreateAllMods().ToList();
            var mods = new List<Mod>();

            foreach (var modString in Mods)
            {
                Mod newMod = availableMods.FirstOrDefault(m => string.Equals(m.Acronym, modString, StringComparison.CurrentCultureIgnoreCase));
                if (newMod == null)
                    throw new ArgumentException($"Invalid mod provided: {modString}");

                mods.Add(newMod);
            }

            return mods.ToArray();
        }
    }
}
