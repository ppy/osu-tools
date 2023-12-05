// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Alba.CsConsoleFormat;
using Humanizer;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using osu.Game.Online.API;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace PerformanceCalculator
{
    [HelpOption("-?|-h|--help")]
    public abstract class ProcessorCommand
    {
        /// <summary>
        /// The console.
        /// </summary>
        public IConsole Console { get; private set; }

        [UsedImplicitly]
        [Option(Template = "-o|--output <file.txt>", Description = "Output results to text file.")]
        public string OutputFile { get; }

        [UsedImplicitly]
        [Option(Template = "-j|--json", Description = "Output results as JSON.")]
        public bool OutputJson { get; }

        public virtual void OnExecute(CommandLineApplication app, IConsole console)
        {
            Console = console;
            Execute();
        }

        public void OutputPerformance(ScoreInfo score, PerformanceAttributes performanceAttributes, DifficultyAttributes difficultyAttributes)
        {
            var result = new Result
            {
                Score = new ScoreStatistics
                {
                    RulesetId = score.Ruleset.OnlineID,
                    BeatmapId = score.BeatmapInfo?.OnlineID ?? -1,
                    Beatmap = score.BeatmapInfo?.ToString() ?? "Unknown beatmap",
                    Mods = score.Mods.Select(m => new APIMod(m)).ToList(),
                    TotalScore = score.TotalScore,
                    LegacyTotalScore = score.LegacyTotalScore ?? 0,
                    Accuracy = score.Accuracy * 100,
                    Combo = score.MaxCombo,
                    Statistics = score.Statistics
                },
                PerformanceAttributes = performanceAttributes,
                DifficultyAttributes = difficultyAttributes
            };

            if (OutputJson)
            {
                string json = JsonConvert.SerializeObject(result, Formatting.Indented);

                Console.WriteLine(json);

                if (OutputFile != null)
                    File.WriteAllText(OutputFile, json);
            }
            else
            {
                var document = new Document();

                AddSectionHeader(document, "Basic score info");

                document.Children.Add(
                    FormatDocumentLine("beatmap", $"{result.Score.BeatmapId} - {result.Score.Beatmap}"),
                    FormatDocumentLine("total score", result.Score.TotalScore.ToString(CultureInfo.InvariantCulture)),
                    FormatDocumentLine("legacy total score", result.Score.LegacyTotalScore.ToString(CultureInfo.InvariantCulture)),
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

        protected string FormatDocumentLine(string name, string value) => $"{name,-20}: {value}\n";

        public void OutputDocument(Document document)
        {
            // todo: make usable by other command
            using (var writer = new StringWriter())
            {
                ConsoleRenderer.RenderDocumentToText(document, new TextRenderTarget(writer), new Rect(0, 0, 250, Size.Infinity));

                var str = writer.GetStringBuilder().ToString();

                var lines = str.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                    lines[i] = lines[i].TrimEnd();
                str = string.Join('\n', lines);

                Console.Write(str);
                if (OutputFile != null)
                    File.WriteAllText(OutputFile, str);
            }
        }

        public virtual void Execute()
        {
        }

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
            public long TotalScore { get; set; }

            [JsonProperty("legacy_total_score")]
            public long LegacyTotalScore { get; set; }

            [JsonProperty("accuracy")]
            public double Accuracy { get; set; }

            [JsonProperty("combo")]
            public int Combo { get; set; }

            [JsonProperty("statistics")]
            public Dictionary<HitResult, int> Statistics { get; set; }
        }
    }
}
