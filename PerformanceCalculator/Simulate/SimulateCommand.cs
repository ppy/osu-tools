// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using Alba.CsConsoleFormat;
using Humanizer;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
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
        [Option(Template = "-j|--json", Description = "Output results as JSON.")]
        public bool OutputJson { get; }

        public override void Execute()
        {
            var ruleset = Ruleset;

            var mods = GetMods(ruleset).ToArray();
            var workingBeatmap = ProcessorWorkingBeatmap.FromFileOrId(Beatmap);
            var beatmap = workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo, mods);

            var beatmapMaxCombo = GetMaxCombo(beatmap);
            var maxCombo = Combo ?? (int)Math.Round(PercentCombo / 100 * beatmapMaxCombo);
            var statistics = GenerateHitResults(Accuracy / 100, beatmap, Misses, Mehs, Goods);
            var score = Score;
            var accuracy = GetAccuracy(statistics);

            var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);
            var difficultyAttributes = difficultyCalculator.Calculate(LegacyHelper.TrimNonDifficultyAdjustmentMods(ruleset, mods).ToArray());
            var performanceCalculator = ruleset.CreatePerformanceCalculator(difficultyAttributes, new ScoreInfo(beatmap.BeatmapInfo, ruleset.RulesetInfo)
            {
                Accuracy = accuracy,
                MaxCombo = maxCombo,
                Statistics = statistics,
                Mods = mods,
                TotalScore = score,
            });

            var ppAttributes = performanceCalculator?.Calculate();

            var result = new Result
            {
                Score = new ScoreStatistics
                {
                    RulesetId = ruleset.RulesetInfo.OnlineID,
                    BeatmapId = workingBeatmap.BeatmapInfo.OnlineID,
                    Beatmap = workingBeatmap.BeatmapInfo.ToString(),
                    Mods = mods.Select(m => new APIMod(m)).ToList(),
                    Score = score,
                    Accuracy = accuracy * 100,
                    Combo = maxCombo,
                    Statistics = statistics
                },
                PerformanceAttributes = ppAttributes,
                DifficultyAttributes = difficultyAttributes
            };

            if (OutputJson)
            {
                string json = JsonConvert.SerializeObject(result);

                Console.Write(json);

                if (OutputFile != null)
                    File.WriteAllText(OutputFile, json);
            }
            else
            {
                var document = new Document();

                AddSectionHeader(document, "Basic score info");

                document.Children.Add(
                    FormatDocumentLine("beatmap", $"{result.Score.BeatmapId} - {result.Score.Beatmap}"),
                    FormatDocumentLine("score", result.Score.Score.ToString(CultureInfo.InvariantCulture)),
                    FormatDocumentLine("accuracy", result.Score.Accuracy.ToString("N2", CultureInfo.InvariantCulture)),
                    FormatDocumentLine("combo", result.Score.Combo.ToString(CultureInfo.InvariantCulture)),
                    FormatDocumentLine("mods", result.Score.Mods.Count > 0 ? result.Score.Mods.Select(m => m.ToString()).Aggregate((c, n) => $"{c}, {n}") : "None")
                );

                AddSectionHeader(document, "Hit statistics");

                foreach (var stat in result.Score.Statistics)
                    document.Children.Add(FormatDocumentLine(stat.Key.ToString().ToLowerInvariant(), stat.Value.ToString(CultureInfo.InvariantCulture)));

                AddSectionHeader(document, "Performance attributes");

                var ppAttributeValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(result.PerformanceAttributes)) ?? new Dictionary<string, object>();
                foreach (var attrib in ppAttributeValues)
                    document.Children.Add(FormatDocumentLine(attrib.Key.Humanize().ToLower(), FormattableString.Invariant($"{attrib.Value:N2}")));

                AddSectionHeader(document, "Difficulty attributes");

                var diffAttributeValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(result.DifficultyAttributes)) ?? new Dictionary<string, object>();
                foreach (var attrib in diffAttributeValues)
                    document.Children.Add(FormatDocumentLine(attrib.Key.Humanize(), FormattableString.Invariant($"{attrib.Value:N2}")));

                OutputDocument(document);
            }
        }

        protected void AddSectionHeader(Document document, string header)
        {
            if (document.Children.Any())
                document.Children.Add(Environment.NewLine);

            document.Children.Add(header);
            document.Children.Add(new Separator());
        }

        protected List<Mod> GetMods(Ruleset ruleset)
        {
            var mods = new List<Mod>();
            if (Mods == null)
                return mods;

            var availableMods = ruleset.CreateAllMods().ToList();

            foreach (var modString in Mods)
            {
                Mod newMod = availableMods.FirstOrDefault(m => string.Equals(m.Acronym, modString, StringComparison.CurrentCultureIgnoreCase));
                if (newMod == null)
                    throw new ArgumentException($"Invalid mod provided: {modString}");

                mods.Add(newMod);
            }

            return mods;
        }

        protected abstract int GetMaxCombo(IBeatmap beatmap);

        protected abstract Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood);

        protected virtual double GetAccuracy(Dictionary<HitResult, int> statistics) => 0;

        protected string FormatDocumentLine(string name, string value) => $"{name.PadRight(20)}: {value}\n";

        private class Result
        {
            [JsonProperty("score")]
            public ScoreStatistics Score { get; set; }

            [JsonProperty("performance_attributes")]
            public PerformanceAttributes PerformanceAttributes { get; set; }

            [JsonProperty("difficulty_attributes")]
            public DifficultyAttributes DifficultyAttributes { get; set; }
        }

        /// <summary>
        /// A trimmed down score.
        /// </summary>
        private class ScoreStatistics
        {
            [JsonProperty("ruleset_id")]
            public int RulesetId { get; set; }

            [JsonProperty("beatmap_id")]
            public int BeatmapId { get; set; }

            [JsonProperty("beatmap")]
            public string Beatmap { get; set; }

            [JsonProperty("mods")]
            public List<APIMod> Mods { get; set; }

            [JsonProperty("total_score")]
            public long Score { get; set; }

            [JsonProperty("accuracy")]
            public double Accuracy { get; set; }

            [JsonProperty("combo")]
            public int Combo { get; set; }

            [JsonProperty("statistics")]
            public Dictionary<HitResult, int> Statistics { get; set; }
        }
    }
}
