// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Newtonsoft.Json;
using osu.Game.Rulesets.Difficulty;

namespace PerformanceCalculatorGUI
{
    internal static class AttributeConversion
    {
        public static Dictionary<string, object> ToDictionary(DifficultyAttributes attributes)
        {
            var attributeValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(attributes)) ?? new Dictionary<string, object>();

            return attributeValues.Select(x => new KeyValuePair<string, object>(x.Key.Humanize().ToLowerInvariant(), x.Value)).ToDictionary(x => x.Key, y => y.Value);
        }

        public static Dictionary<string, object> ToDictionary(PerformanceAttributes attributes)
        {
            var attributeValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(attributes)) ?? new Dictionary<string, object>();

            return attributeValues.Select(x => new KeyValuePair<string, object>(x.Key.Humanize().ToLowerInvariant(), x.Value)).ToDictionary(x => x.Key, y => y.Value);
        }

        public static string ToReadableString(PerformanceAttributes attributes)
        {
            var dictionary = ToDictionary(attributes);

            return string.Join("\n", dictionary.Select(x => $"{x.Key}: {x.Value:N2}"));
        }
    }
}
