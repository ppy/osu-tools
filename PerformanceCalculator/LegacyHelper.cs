// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mania.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Taiko.Difficulty;

namespace PerformanceCalculator
{
    public static class LegacyHelper
    {
        public static Ruleset GetRulesetFromLegacyID(int id)
        {
            switch (id)
            {
                default:
                    throw new ArgumentException("Invalid ruleset ID provided.");

                case 0:
                    return new OsuRuleset();

                case 1:
                    return new TaikoRuleset();

                case 2:
                    return new CatchRuleset();

                case 3:
                    return new ManiaRuleset();
            }
        }

        public static string GetRulesetShortNameFromId(int id)
        {
            switch (id)
            {
                default:
                    throw new ArgumentException("Invalid ruleset ID provided.");

                case 0:
                    return "osu";

                case 1:
                    return "taiko";

                case 2:
                    return "fruits";

                case 3:
                    return "mania";
            }
        }

        public const LegacyMods KEY_MODS = LegacyMods.Key1 | LegacyMods.Key2 | LegacyMods.Key3 | LegacyMods.Key4 | LegacyMods.Key5 | LegacyMods.Key6 | LegacyMods.Key7 | LegacyMods.Key8
                                           | LegacyMods.Key9 | LegacyMods.KeyCoop;

        // See: https://github.com/ppy/osu-queue-score-statistics/blob/2264bfa68e14bb16ec71a7cac2072bdcfaf565b6/osu.Server.Queues.ScoreStatisticsProcessor/Helpers/LegacyModsHelper.cs
        public static LegacyMods MaskRelevantMods(LegacyMods mods, bool isConvertedBeatmap, int rulesetId)
        {
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
                        relevantMods |= KEY_MODS;
                    break;
            }

            return mods & relevantMods;
        }

        /// <summary>
        /// Transforms a given <see cref="Mod"/> combination into one which is applicable to legacy scores.
        /// This is used to match osu!stable/osu!web calculations for the time being, until such a point that these mods do get considered.
        /// </summary>
        public static LegacyMods ConvertToLegacyDifficultyAdjustmentMods(BeatmapInfo beatmapInfo, Ruleset ruleset, Mod[] mods)
        {
            var legacyMods = ruleset.ConvertToLegacyMods(mods);

            // mods that are not represented in `LegacyMods` (but we can approximate them well enough with others)
            if (mods.Any(mod => mod is ModDaycore))
                legacyMods |= LegacyMods.HalfTime;

            return MaskRelevantMods(legacyMods, ruleset.RulesetInfo.OnlineID != beatmapInfo.Ruleset.OnlineID, ruleset.RulesetInfo.OnlineID);
        }

        /// <summary>
        /// Transforms a given <see cref="Mod"/> combination into one which is applicable to legacy scores.
        /// This is used to match osu!stable/osu!web calculations for the time being, until such a point that these mods do get considered.
        /// </summary>
        public static Mod[] FilterDifficultyAdjustmentMods(BeatmapInfo beatmapInfo, Ruleset ruleset, Mod[] mods)
            => ruleset.ConvertFromLegacyMods(ConvertToLegacyDifficultyAdjustmentMods(beatmapInfo, ruleset, mods)).ToArray();

        public static DifficultyAttributes CreateDifficultyAttributes(int legacyId)
        {
            switch (legacyId)
            {
                case 0:
                    return new OsuDifficultyAttributes();

                case 1:
                    return new TaikoDifficultyAttributes();

                case 2:
                    return new CatchDifficultyAttributes();

                case 3:
                    return new ManiaDifficultyAttributes();

                default:
                    throw new ArgumentException($"Invalid ruleset ID: {legacyId}", nameof(legacyId));
            }
        }
    }
}
