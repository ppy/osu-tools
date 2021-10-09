// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using Alba.CsConsoleFormat;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using osu.Framework.IO.Network;
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
        public string Beatmap { get; set; }

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

            if (!Beatmap.EndsWith(".osu"))
            {
                if (!int.TryParse(Beatmap, out _))
                {
                    Console.WriteLine("Incorrect beatmap ID.");
                    return;
                }

                string cachePath = Path.Combine("cache", $"{Beatmap}.osu");

                if (!File.Exists(cachePath))
                {
                    Console.WriteLine($"Downloading {Beatmap}.osu...");
                    new FileWebRequest(cachePath, $"https://osu.ppy.sh/osu/{Beatmap}").Perform();
                }

                Beatmap = cachePath;
            }

            var workingBeatmap = new ProcessorWorkingBeatmap(Beatmap);

            var beatmap = workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo, mods);

            var beatmapMaxCombo = GetMaxCombo(beatmap);
            var maxCombo = Combo ?? (int)Math.Round(PercentCombo / 100 * beatmapMaxCombo);
            var statistics = GenerateHitResults(Accuracy / 100, beatmap, Misses, Mehs, Goods);
            var score = Score;
            var accuracy = GetAccuracy(statistics);

            var scoreInfo = new ScoreInfo
            {
                Accuracy = accuracy,
                MaxCombo = maxCombo,
                Statistics = statistics,
                Mods = mods,
                TotalScore = score,
                RulesetID = Ruleset.RulesetInfo.ID ?? 0
            };

            var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);
            var difficultyAttributes = difficultyCalculator.Calculate(LegacyHelper.TrimNonDifficultyAdjustmentMods(ruleset, scoreInfo.Mods).ToArray());
            var performanceCalculator = ruleset.CreatePerformanceCalculator(difficultyAttributes, scoreInfo);

            var categoryAttribs = new Dictionary<string, double>();
            double pp = performanceCalculator.Calculate(categoryAttribs);

            if (OutputJson)
            {
                var o = new JObject
                {
                    { "Beatmap", workingBeatmap.BeatmapInfo.ToString() }
                };

                foreach (var info in getPlayValues(scoreInfo, beatmap))
                    o[info.Key] = info.Value;

                o["Mods"] = mods.Length > 0 ? mods.Select(m => m.Acronym).Aggregate((c, n) => $"{c}, {n}") : "None";

                foreach (var kvp in categoryAttribs)
                    o[kvp.Key] = kvp.Value;

                o["pp"] = pp;

                string json = o.ToString();

                Console.Write(json);

                if (OutputFile != null)
                    File.WriteAllText(OutputFile, json);
            }
            else
            {
                var document = new Document();

                document.Children.Add(new Span(workingBeatmap.BeatmapInfo.ToString()), "\n");

                document.Children.Add(new Span(GetPlayInfo(scoreInfo, beatmap)), "\n");

                document.Children.Add(new Span(GetAttribute("Mods", mods.Length > 0
                    ? mods.Select(m => m.Acronym).Aggregate((c, n) => $"{c}, {n}")
                    : "None")), "\n");

                foreach (var kvp in categoryAttribs)
                    document.Children.Add(new Span(GetAttribute(kvp.Key, kvp.Value.ToString(CultureInfo.InvariantCulture))), "\n");

                document.Children.Add(new Span(GetAttribute("pp", pp.ToString(CultureInfo.InvariantCulture))));

                OutputDocument(document);
            }
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

        private Dictionary<string, double> getPlayValues(ScoreInfo scoreInfo, IBeatmap beatmap)
        {
            var playInfo = new Dictionary<string, double>
            {
                { "Accuracy", scoreInfo.Accuracy * 100 },
                { "Combo", scoreInfo.MaxCombo },
            };

            foreach (var statistic in scoreInfo.Statistics)
            {
                playInfo.Add(Enum.GetName(typeof(HitResult), statistic.Key), statistic.Value);
            }

            return playInfo;
        }

        protected abstract string GetPlayInfo(ScoreInfo scoreInfo, IBeatmap beatmap);

        protected abstract int GetMaxCombo(IBeatmap beatmap);

        protected abstract Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood);

        protected virtual double GetAccuracy(Dictionary<HitResult, int> statistics) => 0;

        protected string GetAttribute(string name, string value) => $"{name.PadRight(15)}: {value}";
    }
}
