// Copyright (c) 2007-2019 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using osu.Framework.IO.Network;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko;
using osu.Game.Scoring;

namespace PerformanceCalculator.Profile
{
    public class ProfileProcessor : IProcessor
    {
        private readonly ProfileCommand command;

        private const string base_url = "https://osu.ppy.sh";

        public ProfileProcessor(ProfileCommand command)
        {
            this.command = command;
        }

        public void Execute()
        {
            //initializing pp-information-holding sorted list
            var displayPlays = new List<UserPlayInfo>();
            //initialize the information from the top 100 plays, held in a dynamic

            var ruleset = getRuleset(command.Ruleset ?? 0);

            foreach (var play in getJsonFromApi($"get_user_best?k={command.Key}&u={command.ProfileName}&m={command.Ruleset}&limit=100&type=username"))
            {
                string beatmapID = play.beatmap_id;

                string cachePath = Path.Combine("cache", $"{beatmapID}.osu");
                if (!File.Exists(cachePath))
                    new FileWebRequest(cachePath, $"{base_url}/osu/{beatmapID}").Perform();

                Mod[] mods = ruleset.ConvertLegacyMods((LegacyMods)play.enabled_mods).ToArray();

                var working = new ProcessorWorkingBeatmap(cachePath) { Mods = { Value = mods } };

                var score = new ProcessorScoreParser(working).Parse(new ScoreInfo
                {
                    Ruleset = ruleset.RulesetInfo,
                    MaxCombo = play.maxcombo,
                    Mods = mods,
                    Statistics = new Dictionary<HitResult, int>
                    {
                        { HitResult.Perfect, 0 },
                        { HitResult.Great, (int)play.count300 },
                        { HitResult.Good, (int)play.count100 },
                        { HitResult.Ok, 0 },
                        { HitResult.Meh, (int)play.count50 },
                        { HitResult.Miss, (int)play.countmiss }
                    }
                });

                var thisPlay = new UserPlayInfo
                {
                    Beatmap = working.BeatmapInfo,
                    LocalPP = ruleset.CreatePerformanceCalculator(working, score.ScoreInfo).Calculate(),
                    LivePP = play.pp,
                    Mods = mods.Length > 0 ? mods.Select(m => m.Acronym).Aggregate((c, n) => $"{c}, {n}") : "None"
                };

                displayPlays.Add(thisPlay);

                writeAttribute(thisPlay.Beatmap.ToString(), "");
                writeAttribute("Mods", thisPlay.Mods);
                writeAttribute("old/new pp", thisPlay.LivePP.ToString(CultureInfo.InvariantCulture) + " / " + thisPlay.LocalPP.ToString(CultureInfo.InvariantCulture));
            }

            int index = 0;
            double totalLocalPP = displayPlays.OrderByDescending(p => p.LocalPP).Sum(play => Math.Pow(0.95, index++) * play.LocalPP);

            index = 0;
            double totalServerPP = displayPlays.OrderByDescending(p => p.LivePP).Sum(play => Math.Pow(0.95, index++) * play.LivePP);

            if (command.IncludeBonus)
            {
                //get user data (used for bonus pp calculation)
                dynamic userData = getJsonFromApi($"get_user?k={command.Key}&u={command.ProfileName}&m={command.Ruleset}&type=username");

                double bonusPP = 0;
                //inactive players have 0pp to take them out of the leaderboard
                if (userData[0].pp_raw == 0)
                    command.Console.WriteLine("The player has 0 pp or is inactive, so bonus pp cannot be calculated");
                //calculate bonus pp as difference of user pp and sum of other pps
                else
                {
                    bonusPP = userData[0].pp_raw - totalServerPP;
                    totalServerPP = userData[0].pp_raw;
                }

                //add on bonus pp
                totalLocalPP += bonusPP;
            }

            writeAttribute("Top 100 Listed Above. Old/New Net PP", totalServerPP.ToString(CultureInfo.InvariantCulture) + " / " + totalLocalPP.ToString(CultureInfo.InvariantCulture));
        }

        private void writeAttribute(string name, string value) => command.Console.WriteLine($"{name.PadRight(15)}: {value}");

        private dynamic getJsonFromApi(string request)
        {
            var req = new JsonWebRequest<dynamic>($"{base_url}/api/{request}");
            req.Perform();
            return req.ResponseObject;
        }

        private Ruleset getRuleset(int rulesetId)
        {
            switch (rulesetId)
            {
                default:
                    throw new ArgumentException("Invalid ruleset id provided.");
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
    }
}
