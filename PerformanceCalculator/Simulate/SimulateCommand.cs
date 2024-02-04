// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
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
        public string Beatmap { get; }

        [UsedImplicitly]
        public virtual double Accuracy { get; }

        [UsedImplicitly]
        public virtual int? Combo { get; }

        [UsedImplicitly]
        public virtual double PercentCombo { get; }

        [UsedImplicitly]
        public virtual int Score { get; }

        [UsedImplicitly]
        public virtual string[] Mods { get; }

        [UsedImplicitly]
        public virtual int Misses { get; }

        [UsedImplicitly]
        public virtual int? Mehs { get; }

        [UsedImplicitly]
        public virtual int? Goods { get; }

        [UsedImplicitly]
        [Option(Template = "-nc|--no-classic", Description = "Excludes the classic mod.")]
        public bool NoClassicMod { get; }

        public override void Execute()
        {
            var ruleset = Ruleset;

            var workingBeatmap = ProcessorWorkingBeatmap.FromFileOrId(Beatmap);
            var mods = NoClassicMod ? GetMods(ruleset) : LegacyHelper.ConvertToLegacyDifficultyAdjustmentMods(workingBeatmap.BeatmapInfo, ruleset, GetMods(ruleset));
            var beatmap = workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo, mods);

            var beatmapMaxCombo = GetMaxCombo(beatmap);
            var statistics = GenerateHitResults(Accuracy / 100, beatmap, Misses, Mehs, Goods);
            var scoreInfo = new ScoreInfo(beatmap.BeatmapInfo, ruleset.RulesetInfo)
            {
                Accuracy = GetAccuracy(statistics),
                MaxCombo = Combo ?? (int)Math.Round(PercentCombo / 100 * beatmapMaxCombo),
                Statistics = statistics,
                Mods = mods,
                TotalScore = Score,
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

        protected abstract int GetMaxCombo(IBeatmap beatmap);

        protected abstract Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood);

        protected virtual double GetAccuracy(Dictionary<HitResult, int> statistics) => 0;
    }
}
