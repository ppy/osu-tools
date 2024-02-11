// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring.Legacy;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;

namespace PerformanceCalculator.Performance
{
    [Command(Name = "legacy-score", Description = "Computes the performance (pp) of an online score.")]
    public class LegacyScorePerformanceCommand : ScorePerformanceCommand
    {
        [Argument(1, "ruleset-id", "The ID of the ruleset that the score was set on.")]
        public int RulesetId { get; set; }

        protected override SoloScoreInfo QueryScore() => GetJsonFromApi<SoloScoreInfo>($"scores/{LegacyHelper.GetRulesetShortNameFromId(RulesetId)}/{ScoreId}");

        protected override ScoreInfo CreateScore(SoloScoreInfo apiScore, Ruleset ruleset, APIBeatmap apiBeatmap, WorkingBeatmap workingBeatmap)
        {
            var score = base.CreateScore(apiScore, ruleset, apiBeatmap, workingBeatmap);

            score.Mods = score.Mods.Append(ruleset.CreateMod<ModClassic>()).ToArray();
            score.IsLegacyScore = true;
            score.LegacyTotalScore = (int)score.TotalScore;
            LegacyScoreDecoder.PopulateMaximumStatistics(score, workingBeatmap);
            StandardisedScoreMigrationTools.UpdateFromLegacy(
                score,
                ruleset,
                LegacyBeatmapConversionDifficultyInfo.FromAPIBeatmap(apiBeatmap),
                ((ILegacyRuleset)ruleset).CreateLegacyScoreSimulator().Simulate(workingBeatmap, workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo, score.Mods)));

            return score;
        }
    }
}
