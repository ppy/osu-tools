// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace PerformanceCalculator.Simulate
{
    public abstract class SimulateCommand : ProcessorCommand
    {
        public abstract Ruleset Ruleset { get; }

        [UsedImplicitly]
        [Required]
        [Argument(0, Name = "beatmap", Description = "Required. Can be either a path to beatmap file (.osu) or beatmap ID.")]
        public string Beatmap { get; set; }

        [UsedImplicitly]
        [Option(Template = "-a|--accuracy <accuracy>", Description = "Accuracy. Enter as decimal 0-100. Defaults to 100. Scales hit results as well and is rounded to the nearest possible value for the beatmap.")]
        public double Accuracy { get; set; } = 100;

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-m|--mod <mod>", Description = "One for each mod. The mods to compute the performance with. Values: hr, dt, hd, fl, etc...")]
        public string[] Mods { get; set; }

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-o|--mod-option <option>",
            Description = "The options of mods, with one for each setting. Specified as acryonym_settingkey=value. Example: DT_speed_change=1.35")]
        public string[] ModOptions { get; set; } = [];

        [UsedImplicitly]
        [Option(Template = "-X|--misses <misses>", Description = "Number of misses. Defaults to 0.")]
        public int Misses { get; set; }

        //
        // Options implemented in the ruleset-specific commands
        // -> Catch renames Mehs/Goods to (tiny-)droplets
        // -> Mania does not have Combo
        // -> Taiko does not have Mehs
        //
        [UsedImplicitly]
        public virtual int? Mehs { get; set; }

        [UsedImplicitly]
        public virtual int? Goods { get; set; }

        [UsedImplicitly]
        public virtual int? Combo { get; }

        [UsedImplicitly]
        public virtual double PercentCombo { get; }

        public override void Execute()
        {
            var ruleset = Ruleset;

            var workingBeatmap = ProcessorWorkingBeatmap.FromFileOrId(Beatmap);
            var mods = GetMods(ruleset);
            var beatmap = workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo, mods);

            var beatmapMaxCombo = GetMaxCombo(beatmap);
            var statistics = GenerateHitResults(Accuracy / 100, beatmap, Misses, Mehs, Goods);
            var scoreInfo = new ScoreInfo(beatmap.BeatmapInfo, ruleset.RulesetInfo)
            {
                Accuracy = GetAccuracy(statistics),
                MaxCombo = Combo ?? (int)Math.Round(PercentCombo / 100 * beatmapMaxCombo),
                Statistics = statistics,
                Mods = mods
            };

            var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);
            var difficultyAttributes = difficultyCalculator.Calculate(mods);
            var performanceCalculator = ruleset.CreatePerformanceCalculator();
            var performanceAttributes = performanceCalculator?.Calculate(scoreInfo, difficultyAttributes);

            OutputPerformance(scoreInfo, performanceAttributes, difficultyAttributes);
        }

        protected Mod[] GetMods(Ruleset ruleset)
        {
            if (Mods == null)
                return Array.Empty<Mod>();

            var mods = new List<Mod>();

            foreach (var modString in Mods)
            {
                APIMod mod = new APIMod { Acronym = modString };

                foreach (string modOption in ModOptions.Where(x => x.StartsWith($"{modString}_", StringComparison.CurrentCultureIgnoreCase)))
                {
                    string[] split = modOption[3..].Split('=');
                    if (split.Length != 2)
                        throw new ArgumentException($"Invalid mod-option format (key=value): {modOption[3..]}");

                    mod.Settings[split[0]] = split[1];
                }

                mods.Add(mod.ToMod(ruleset));
            }

            return mods.ToArray();
        }

        protected abstract int GetMaxCombo(IBeatmap beatmap);

        protected abstract Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood);

        protected virtual double GetAccuracy(Dictionary<HitResult, int> statistics) => 0;
    }
}
