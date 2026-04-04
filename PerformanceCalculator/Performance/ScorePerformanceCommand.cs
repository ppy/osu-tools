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
using osu.Game.Models;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mania.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Taiko.Difficulty;
using osu.Game.Scoring;

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
            var workingBeatmap = ProcessorWorkingBeatmap.FromFileOrId(apiScore.BeatmapID.ToString());
            var score = CreateScore(apiScore, ruleset, apiBeatmap, workingBeatmap);

            DifficultyAttributes attributes;

            if (OnlineAttributes)
            {
                LegacyMods legacyMods = convertToLegacyMods(workingBeatmap.BeatmapInfo, ruleset, score.Mods);
                attributes = queryApiAttributes(apiScore.BeatmapID, apiScore.RulesetID, legacyMods);
            }
            else
            {
                var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);
                attributes = difficultyCalculator.Calculate(score.Mods);
            }

            var performanceCalculator = ruleset.CreatePerformanceCalculator();
            var performanceAttributes = performanceCalculator?.Calculate(score, attributes);

            OutputPerformance(score, performanceAttributes, attributes);
        }

        protected virtual SoloScoreInfo QueryScore() => GetJsonFromApi<SoloScoreInfo>($"scores/{ScoreId}");

        protected virtual ScoreInfo CreateScore(SoloScoreInfo apiScore, Ruleset ruleset, APIBeatmap apiBeatmap, WorkingBeatmap workingBeatmap)
        {
            var score = apiScore.ToScoreInfo(apiScore.Mods.Select(m => m.ToMod(ruleset)).ToArray(), apiBeatmap);
            score.Ruleset = ruleset.RulesetInfo;
            score.BeatmapInfo!.Metadata = new BeatmapMetadata
            {
                Title = apiBeatmap.Metadata.Title,
                Artist = apiBeatmap.Metadata.Artist,
                Author = new RealmUser { Username = apiBeatmap.Metadata.Author.Username },
            };

            return score;
        }

        private DifficultyAttributes queryApiAttributes(int beatmapId, int rulesetId, LegacyMods mods)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "mods", ((int)mods).ToString(CultureInfo.InvariantCulture) }
            };

            var beatmap = GetJsonFromApi<APIBeatmap>($"beatmaps/{beatmapId}");

            switch (rulesetId)
            {
                case 0:
                    return getMergedAttributes<OsuDifficultyAttributes>(beatmap);

                case 1:
                    return getMergedAttributes<TaikoDifficultyAttributes>(beatmap);

                case 2:
                    return getMergedAttributes<CatchDifficultyAttributes>(beatmap);

                case 3:
                    return getMergedAttributes<ManiaDifficultyAttributes>(beatmap);

                default:
                    throw new ArgumentOutOfRangeException(nameof(rulesetId));
            }

            DifficultyAttributes getMergedAttributes<TAttributes>(APIBeatmap apiBeatmap)
                where TAttributes : DifficultyAttributes, new()
            {
                // the osu-web endpoint queries osu-beatmap-difficulty-cache, which in turn does not return the full set of attributes -
                // it skips ones that are already present on `APIBeatmap`
                // (https://github.com/ppy/osu-beatmap-difficulty-lookup-cache/blob/db2203368221109803f2031788da31deb94e0f11/BeatmapDifficultyLookupCache/DifficultyCache.cs#L125-L128).
                // to circumvent this, do some manual grafting on our side to produce a fully populated set of attributes.
                var databasedAttributes = GetJsonFromApi<AttributesResponse<TAttributes>>($"beatmaps/{beatmapId}/attributes", HttpMethod.Post, parameters).Attributes;
                var fullAttributes = new TAttributes();
                fullAttributes.FromDatabaseAttributes(databasedAttributes.ToDatabaseAttributes().ToDictionary(
                    pair => pair.attributeId,
                    pair => Convert.ToDouble(pair.value, CultureInfo.InvariantCulture)
                ), apiBeatmap);
                return fullAttributes;
            }
        }

        /// <summary>
        /// Transforms a given <see cref="Mod"/> combination into one which is applicable to legacy scores.
        /// This should only be used to match performance calculations using databased attributes.
        /// </summary>
        private static LegacyMods convertToLegacyMods(BeatmapInfo beatmapInfo, Ruleset ruleset, Mod[] mods)
        {
            var legacyMods = ruleset.ConvertToLegacyMods(mods);

            // mods that are not represented in `LegacyMods` (but we can approximate them well enough with others)
            if (mods.Any(mod => mod is ModDaycore))
                legacyMods |= LegacyMods.HalfTime;

            // See: https://github.com/ppy/osu-queue-score-statistics/blob/2264bfa68e14bb16ec71a7cac2072bdcfaf565b6/osu.Server.Queues.ScoreStatisticsProcessor/Helpers/LegacyModsHelper.cs
            static LegacyMods maskRelevantMods(LegacyMods mods, bool isConvertedBeatmap, int rulesetId)
            {
                const LegacyMods key_mods = LegacyMods.Key1 | LegacyMods.Key2 | LegacyMods.Key3 | LegacyMods.Key4 | LegacyMods.Key5 | LegacyMods.Key6 | LegacyMods.Key7 | LegacyMods.Key8
                                            | LegacyMods.Key9 | LegacyMods.KeyCoop;

                LegacyMods relevantMods = LegacyMods.DoubleTime | LegacyMods.HalfTime | LegacyMods.HardRock | LegacyMods.Easy;

                switch (rulesetId)
                {
                    case 0:
                        if ((mods & LegacyMods.Flashlight) > 0)
                            relevantMods |= LegacyMods.Flashlight | LegacyMods.Hidden | LegacyMods.TouchDevice;
                        else
                            relevantMods |= LegacyMods.Flashlight | LegacyMods.TouchDevice;
                        break;

                    case 3:
                        if (isConvertedBeatmap)
                            relevantMods |= key_mods;
                        break;
                }

                return mods & relevantMods;
            }

            return maskRelevantMods(legacyMods, ruleset.RulesetInfo.OnlineID != beatmapInfo.Ruleset.OnlineID, ruleset.RulesetInfo.OnlineID);
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class AttributesResponse<T>
            where T : DifficultyAttributes
        {
            [JsonProperty("attributes")]
            public required T Attributes { get; set; }
        }
    }
}
