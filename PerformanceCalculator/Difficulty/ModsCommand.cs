// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Humanizer;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace PerformanceCalculator.Difficulty
{
    [Command(Name = "mods", Description = "Outputs all available mods in a consumable format.")]
    public class ModsCommand : ProcessorCommand
    {
        public override void Execute()
        {
            var allRulesets = Enumerable.Range(0, ILegacyRuleset.MAX_LEGACY_RULESET_ID + 1)
                                        .Select(LegacyHelper.GetRulesetFromLegacyID);

            Console.WriteLine(JsonConvert.SerializeObject(allRulesets.Select(r => new
            {
                Name = r.RulesetInfo.ShortName,
                RulesetID = r.RulesetInfo.OnlineID,
                Mods = getDefinitionsForRuleset(r)
            }), Formatting.Indented));
        }

        private IEnumerable<dynamic> getDefinitionsForRuleset(Ruleset ruleset)
        {
            var allMods = ruleset.CreateAllMods();

            return allMods.Select(mod => new
            {
                mod.Acronym,
                mod.Name,
                Description = mod.Description.ToString(),
                Type = mod.Type.ToString(),
                Settings = getSettingsDefinitions(mod),
                IncompatibleMods = getAllImplementations(mod.IncompatibleMods),
                mod.RequiresConfiguration,
                mod.UserPlayable,
                mod.ValidForMultiplayer,
                mod.ValidForMultiplayerAsFreeMod,
                mod.AlwaysValidForSubmission,
            });

            IEnumerable<string> getAllImplementations(Type[] incompatibleTypes)
            {
                foreach (var mod in allMods)
                {
                    if (incompatibleTypes.Any(t => t.IsInstanceOfType(mod)))
                        yield return mod.Acronym;
                }
            }

            IEnumerable<dynamic> getSettingsDefinitions(Mod mod)
            {
                var sourceProperties = mod.GetSettingsSourceProperties();

                foreach (var (settingsSource, propertyInfo) in sourceProperties)
                {
                    var bindable = propertyInfo.GetValue(mod);

                    Debug.Assert(bindable != null);

                    object? underlyingValue = (object?)bindable.GetUnderlyingSettingValue();
                    var netType = underlyingValue?.GetType() ?? bindable.GetType().GetInterface("IBindable`1")?.GenericTypeArguments.FirstOrDefault();

                    yield return new
                    {
                        Name = propertyInfo.Name.Underscore(),
                        Type = getJsonType(netType),
                        Label = settingsSource.Label.ToString(),
                        Description = settingsSource.Description.ToString(),
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
