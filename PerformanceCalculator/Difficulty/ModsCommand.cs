// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using Humanizer;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;

namespace PerformanceCalculator.Difficulty
{
    [Command(Name = "mods", Description = "Outputs all available mods in a consumable format.")]
    public class ModsCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required]
        [Argument(
            0,
            Name = "ruleset",
            Description = "The ruleset to compute the beatmap difficulty for, if it's a convertible beatmap.\n"
                          + "Values: 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania")]
        [AllowedValues("0", "1", "2", "3")]
        public int Ruleset { get; }

        public override void Execute()
        {
            var ruleset = LegacyHelper.GetRulesetFromLegacyID(Ruleset);

            var allMods = ruleset.CreateAllMods();

            var modDefinitions = allMods.Select(mod => new
            {
                mod.Acronym,
                mod.Name,
                mod.Description,
                Type = mod.Type.ToString(),
                Settings = getSettingsDefinitions(mod),
                IncompatibleMods = getAllImplementations(mod.IncompatibleMods),
                mod.RequiresConfiguration,
                mod.UserPlayable,
                mod.ValidForMultiplayer,
                mod.ValidForMultiplayerAsFreeMod,
            });

            Console.WriteLine(JsonConvert.SerializeObject(modDefinitions, Formatting.Indented));

            IEnumerable<string> getAllImplementations(Type[] incompatibleTypes)
            {
                foreach (var mod in allMods)
                {
                    if (incompatibleTypes.Any(t => mod.GetType().IsSubclassOf(t)))
                        yield return mod.Acronym;
                }
            }

            IEnumerable<dynamic> getSettingsDefinitions(Mod mod)
            {
                var sourceProperties = mod.GetSettingsSourceProperties();

                foreach (var (settingSourceAttribute, propertyInfo) in sourceProperties)
                {
                    var bindable = propertyInfo.GetValue(mod);

                    Debug.Assert(bindable != null);

                    object? underlyingValue = bindable.GetUnderlyingSettingValue();

                    var netType = underlyingValue?.GetType() ?? bindable.GetType().GetInterface("IBindable`1")?.GenericTypeArguments.FirstOrDefault();

                    yield return new
                    {
                        Name = propertyInfo.Name.Underscore(),
                        Type = getJsonType(netType)
                    };
                }
            }
        }

        private string getJsonType(Type? netType)
        {
            if (netType == typeof(int))
                return "number";
            if (netType == typeof(double))
                return "number";
            if (netType == typeof(float))
                return "number";
            if (netType == typeof(int?))
                return "number";
            if (netType == typeof(double?))
                return "number";
            if (netType == typeof(float?))
                return "number";

            if (netType == typeof(bool))
                return "boolean";
            if (netType == typeof(bool?))
                return "boolean";

            if (netType == typeof(string))
                return "string";

            if (netType?.IsEnum == true)
                return "string";

            throw new ArgumentOutOfRangeException(nameof(netType));
        }
    }
}
