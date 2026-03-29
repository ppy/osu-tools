// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Database;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets.Scoring;

namespace PerformanceCalculatorGUI.Screens.Collections
{
    public class StoredScore
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public int BeatmapID { get; set; }

        public int RulesetID { get; set; }

        public double Accuracy { get; set; }

        public int MaxCombo { get; set; }

        public Dictionary<HitResult, int> Statistics { get; set; } = new Dictionary<HitResult, int>();

        public APIMod[] Mods { get; set; } = Array.Empty<APIMod>();

        public long TotalScore { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        public SoloScoreInfo ToSoloScoreInfo(ProcessorWorkingBeatmap working)
        {
            var metadata = working.BeatmapInfo.Metadata;
            var rulesetInstance = RulesetHelper.GetRulesetFromLegacyID(RulesetID);
            var mods = Mods.Select(m => m.ToMod(rulesetInstance)).ToArray();
            var rank = StandardisedScoreMigrationTools.ComputeRank(Accuracy, Statistics, mods, rulesetInstance.CreateScoreProcessor());

            return new SoloScoreInfo
            {
                BeatmapID = BeatmapID,
                RulesetID = RulesetID,
                Accuracy = Accuracy,
                MaxCombo = MaxCombo,
                Statistics = new Dictionary<HitResult, int>(Statistics),
                Mods = Mods,
                TotalScore = TotalScore,
                Passed = true,
                Rank = rank,
                EndedAt = CreatedAt,
                Beatmap = new APIBeatmap
                {
                    OnlineID = working.BeatmapInfo.OnlineID,
                    DifficultyName = working.BeatmapInfo.DifficultyName,
                    RulesetID = RulesetID,
                    BeatmapSet = new APIBeatmapSet
                    {
                        OnlineID = working.BeatmapInfo.BeatmapSet?.OnlineID ?? 0,
                        Title = metadata.Title,
                        TitleUnicode = metadata.TitleUnicode,
                        Artist = metadata.Artist,
                        ArtistUnicode = metadata.ArtistUnicode
                    }
                }
            };
        }
    }
}
