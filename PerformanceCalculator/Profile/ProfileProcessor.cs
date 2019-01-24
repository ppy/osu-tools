// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alba.CsConsoleFormat;
using osu.Framework.IO.Network;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
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

            var ruleset = LegacyHelper.GetRulesetFromLegacyID(command.Ruleset ?? 0);

            foreach (var play in getJsonFromApi($"get_user_best?k={command.Key}&u={command.ProfileName}&m={command.Ruleset}&limit=100&type=username"))
            {
                string beatmapID = play.beatmap_id;

                string cachePath = Path.Combine("cache", $"{beatmapID}.osu");
                if (!File.Exists(cachePath))
                    new FileWebRequest(cachePath, $"{base_url}/osu/{beatmapID}").Perform();

                Mod[] mods = ruleset.ConvertLegacyMods((LegacyMods)play.enabled_mods).ToArray();

                var working = new ProcessorWorkingBeatmap(cachePath, (int)play.beatmap_id) { Mods = { Value = mods } };

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
            }

            dynamic userData = getJsonFromApi($"get_user?k={command.Key}&u={command.ProfileName}&m={command.Ruleset}&type=username")[0];

            var localOrdered = displayPlays.OrderByDescending(p => p.LocalPP).ToList();
            var liveOrdered = displayPlays.OrderByDescending(p => p.LivePP).ToList();

            int index = 0;
            double totalLocalPP = localOrdered.Sum(play => Math.Pow(0.95, index++) * play.LocalPP);
            double totalLivePP = userData.pp_raw;

            index = 0;
            double nonBonusLivePP = liveOrdered.Sum(play => Math.Pow(0.95, index++) * play.LivePP);

            //todo: implement properly. this is pretty damn wrong.
            var playcountBonusPP = (totalLivePP - nonBonusLivePP);
            totalLocalPP += playcountBonusPP;

            outputDocument(new Document(
                new Span($"User:     {userData.username}"), "\n",
                new Span($"Live PP:  {totalLivePP:F1} (including {playcountBonusPP:F1}pp from playcount)"), "\n",
                new Span($"Local PP: {totalLocalPP:F1}"), "\n",
                new Grid
                {
                    Columns = { GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto },
                    Children =
                    {
                        new Cell("beatmap"),
                        new Cell("live pp"),
                        new Cell("local pp"),
                        new Cell("pp change"),
                        new Cell("position change"),
                        localOrdered.Select(item => new[]
                        {
                            new Cell($"{item.Beatmap.OnlineBeatmapID} - {item.Beatmap}"),
                            new Cell($"{item.LivePP:F1}") { Align = Align.Right },
                            new Cell($"{item.LocalPP:F1}") { Align = Align.Right },
                            new Cell($"{item.LocalPP - item.LivePP:F1}") { Align = Align.Right },
                            new Cell($"{liveOrdered.IndexOf(item) - localOrdered.IndexOf(item):+0;-0;-}") { Align = Align.Center },
                        })
                    }
                }
            ));
        }

        private void outputDocument(Document document)
        {
            // todo: make usable by other command
            using (var writer = new StringWriter())
            {
                ConsoleRenderer.RenderDocumentToText(document, new TextRenderTarget(writer));

                var str = writer.GetStringBuilder().ToString();

                var lines = str.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                    lines[i] = lines[i].TrimEnd();
                str = string.Join('\n', lines);

                Console.Write(str);
                if (command.OutputFile != null)
                    File.WriteAllText(command.OutputFile, str);
            }
        }

        private dynamic getJsonFromApi(string request)
        {
            var req = new JsonWebRequest<dynamic>($"{base_url}/api/{request}");
            req.Perform();
            return req.ResponseObject;
        }
    }
}
