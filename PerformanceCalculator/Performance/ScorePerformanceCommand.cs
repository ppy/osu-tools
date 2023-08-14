// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Models;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets.Mods;

namespace PerformanceCalculator.Performance
{
    [Command(Name = "score", Description = "Computes the performance (pp) of an online score.")]
    public class ScorePerformanceCommand : ApiCommand
    {
        [Argument(0, "ruleset-id", "The ID of the ruleset that the score was set on.")]
        public int RulesetId { get; set; }

        [Argument(1, "score-id", "The score's online ID.")]
        public ulong ScoreId { get; set; }

        public override void Execute()
        {
            base.Execute();

            SoloScoreInfo apiScore = GetJsonFromApi<SoloScoreInfo>($"scores/{LegacyHelper.GetRulesetShortNameFromId(RulesetId)}/{ScoreId}");
            APIBeatmap apiBeatmap = GetJsonFromApi<APIBeatmap>($"beatmaps/lookup?id={apiScore.BeatmapID}");

            var ruleset = LegacyHelper.GetRulesetFromLegacyID(apiScore.RulesetID);
            var score = apiScore.ToScoreInfo(apiScore.Mods.Select(m => m.ToMod(ruleset)).ToArray(), apiBeatmap);
            score.BeatmapInfo.Metadata = new BeatmapMetadata
            {
                Title = apiBeatmap.Metadata.Title,
                Artist = apiBeatmap.Metadata.Artist,
                Author = new RealmUser { Username = apiBeatmap.Metadata.Author.Username },
            };

            if (apiScore.BuildID == null)
                score.Mods = score.Mods.Append(ruleset.CreateMod<ModClassic>()).ToArray();

            var workingBeatmap = ProcessorWorkingBeatmap.FromFileOrId(score.BeatmapInfo!.OnlineID.ToString());
            var difficultyCalculator = ruleset.CreateDifficultyCalculator(workingBeatmap);
            var difficultyAttributes = difficultyCalculator.Calculate(LegacyHelper.ConvertToLegacyDifficultyAdjustmentMods(ruleset, score.Mods));
            var performanceCalculator = ruleset.CreatePerformanceCalculator();
            var performanceAttributes = performanceCalculator?.Calculate(score, difficultyAttributes);

            OutputPerformance(score, performanceAttributes, difficultyAttributes);
        }
    }
}
