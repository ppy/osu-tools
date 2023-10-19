// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;
using osu.Game.Skinning;
using osu.Game.Utils;

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

        /// <summary>
        /// Transforms a given <see cref="Mod"/> combination into one which is applicable to legacy scores.
        /// This is used to match osu!stable/osu!web calculations for the time being, until such a point that these mods do get considered.
        /// </summary>
        public static Mod[] ConvertToLegacyDifficultyAdjustmentMods(BeatmapInfo beatmapInfo, Ruleset ruleset, Mod[] mods)
        {
            var allMods = ruleset.CreateAllMods().ToArray();

            var allowedMods = ModUtils.FlattenMods(
                                          ruleset.CreateDifficultyCalculator(new EmptyWorkingBeatmap(beatmapInfo)).CreateDifficultyAdjustmentModCombinations())
                                      .Select(m => m.GetType())
                                      .Distinct()
                                      .ToHashSet();

            // Special case to allow either DT or NC.
            if (allowedMods.Any(type => type.IsSubclassOf(typeof(ModDoubleTime))) && mods.Any(m => m is ModNightcore))
                allowedMods.Add(allMods.Single(m => m is ModNightcore).GetType());

            var result = new List<Mod>();

            var classicMod = allMods.SingleOrDefault(m => m is ModClassic);
            if (classicMod != null)
                result.Add(classicMod);

            result.AddRange(mods.Where(m => allowedMods.Contains(m.GetType())));

            return result.ToArray();
        }

        private class EmptyWorkingBeatmap : WorkingBeatmap
        {
            public EmptyWorkingBeatmap(BeatmapInfo beatmapInfo)
                : base(beatmapInfo, null)
            {
            }

            protected override IBeatmap GetBeatmap() => throw new NotImplementedException();

            public override Texture GetBackground() => throw new NotImplementedException();

            protected override Track GetBeatmapTrack() => throw new NotImplementedException();

            protected override ISkin GetSkin() => throw new NotImplementedException();

            public override Stream GetStream(string storagePath) => throw new NotImplementedException();
        }
    }
}
