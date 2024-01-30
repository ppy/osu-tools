// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Database;
using osu.Game.Models;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mania.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Scoring.Legacy;
using osu.Game.Rulesets.Taiko.Difficulty;

namespace PerformanceCalculator.Performance
{
    [Command(Name = "score", Description = "Computes the performance (pp) of an online score.")]
    public class ScorePerformanceCommand : ApiCommand
    {
        [Argument(0, "score-id", "The score's online ID.")]
        public ulong ScoreId { get; set; }

        [Option(CommandOptionType.NoValue, Template = "-a|--online-attributes", Description = "Whether to use the currently-live difficulty attributes for the beatmap.")]
        public bool OnlineAttributes { get; set; }

        public override void Execute()
        {
            base.Execute();

            SoloScoreInfo apiScore = QueryScore();
            APIBeatmap apiBeatmap = GetJsonFromApi<APIBeatmap>($"beatmaps/lookup?id={apiScore.BeatmapID}");

            var ruleset = LegacyHelper.GetRulesetFromLegacyID(apiScore.RulesetID);
            var score = apiScore.ToScoreInfo(apiScore.Mods.Select(m => m.ToMod(ruleset)).ToArray(), apiBeatmap);
            score.Ruleset = ruleset.RulesetInfo;
            score.BeatmapInfo!.Metadata = new BeatmapMetadata
            {
                Title = apiBeatmap.Metadata.Title,
                Artist = apiBeatmap.Metadata.Artist,
                Author = new RealmUser { Username = apiBeatmap.Metadata.Author.Username },
            };

            var workingBeatmap = ProcessorWorkingBeatmap.FromFileOrId(score.BeatmapInfo!.OnlineID.ToString());

            if (apiScore.BuildID == null)
            {
                score.Mods = score.Mods.Append(ruleset.CreateMod<ModClassic>()).ToArray();
                score.IsLegacyScore = true;
                score.LegacyTotalScore = (int)score.TotalScore;
                StandardisedScoreMigrationTools.UpdateFromLegacy(
                    score,
                    LegacyBeatmapConversionDifficultyInfo.FromAPIBeatmap(apiBeatmap),
                    ((ILegacyRuleset)ruleset).CreateLegacyScoreSimulator().Simulate(workingBeatmap, workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo, score.Mods)));
            }

            DifficultyAttributes attributes;

            if (OnlineAttributes)
            {
                LegacyMods legacyMods = LegacyHelper.ConvertToLegacyDifficultyAdjustmentMods(workingBeatmap.BeatmapInfo, ruleset, score.Mods);
                attributes = queryApiAttributes(apiScore.BeatmapID, apiScore.RulesetID, legacyMods);
            }
            else
            {
                var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);
                attributes = difficultyCalculator.Calculate(LegacyHelper.FilterDifficultyAdjustmentMods(workingBeatmap.BeatmapInfo, ruleset, score.Mods));
            }

            var performanceCalculator = ruleset.CreatePerformanceCalculator();
            var performanceAttributes = performanceCalculator?.Calculate(score, attributes);

            OutputPerformance(score, performanceAttributes, attributes);
        }

        protected virtual SoloScoreInfo QueryScore() => GetJsonFromApi<SoloScoreInfo>($"scores/{ScoreId}");

        private DifficultyAttributes queryApiAttributes(int beatmapId, int rulesetId, LegacyMods mods)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "mods", ((int)mods).ToString(CultureInfo.InvariantCulture) }
            };

            switch (rulesetId)
            {
                case 0:
                    return GetJsonFromApi<AttributesResponse<OsuDifficultyAttributes>>($"beatmaps/{beatmapId}/attributes", HttpMethod.Post, parameters).Attributes;

                case 1:
                    return GetJsonFromApi<AttributesResponse<TaikoDifficultyAttributes>>($"beatmaps/{beatmapId}/attributes", HttpMethod.Post, parameters).Attributes;

                case 2:
                    return GetJsonFromApi<AttributesResponse<CatchDifficultyAttributes>>($"beatmaps/{beatmapId}/attributes", HttpMethod.Post, parameters).Attributes;

                case 3:
                    return GetJsonFromApi<AttributesResponse<ManiaDifficultyAttributes>>($"beatmaps/{beatmapId}/attributes", HttpMethod.Post, parameters).Attributes;

                default:
                    throw new ArgumentOutOfRangeException(nameof(rulesetId));
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class AttributesResponse<T>
            where T : DifficultyAttributes
        {
            [JsonProperty("attributes")]
            public T Attributes { get; set; }
        }
    }
}
