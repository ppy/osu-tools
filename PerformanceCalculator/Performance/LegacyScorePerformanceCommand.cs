// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using McMaster.Extensions.CommandLineUtils;
using osu.Game.Online.API.Requests.Responses;

namespace PerformanceCalculator.Performance
{
    [Command(Name = "legacy-score", Description = "Computes the performance (pp) of an online score.")]
    public class LegacyScorePerformanceCommand : ScorePerformanceCommand
    {
        [Argument(1, "ruleset-id", "The ID of the ruleset that the score was set on.")]
        public int RulesetId { get; set; }

        protected override SoloScoreInfo QueryScore() => GetJsonFromApi<SoloScoreInfo>($"scores/{LegacyHelper.GetRulesetShortNameFromId(RulesetId)}/{ScoreId}");
    }
}
