// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using Newtonsoft.Json;

namespace PerformanceCalculator.Profile
{
    public class ProfileProcessor : IProcessor
    {

        private readonly ProfileCommand command;
        public ProfileProcessor(ProfileCommand command)
        {
            this.command = command;
        }

        public void Execute()
        {
            //initializing pp-holding array
            double[] pp = new double[100];
            
            //get data for all 100 top plays
            var getPlayData = (HttpWebRequest) WebRequest.Create("https://osu.ppy.sh/api/get_user_best?k="+command.Key+"&u="+command.ProfileName+"&limit=100&type=username");
            HttpWebResponse response = (HttpWebResponse)getPlayData.GetResponse();
            string json = null;
            var receiveStream = response.GetResponseStream();
            var readStream = new StreamReader(receiveStream);
            json = readStream.ReadToEnd();
            var playData = JsonConvert.DeserializeObject<dynamic>(json);
            response.Close();
            receiveStream.Close();
            readStream.Close();

            for (var i = 0; i<100; i++)
            {
                //if (!File.Exists(command.Path))
                    //File.Create(command.Path);
                //for each beatmap, download it
                using (var client = new WebClient()) {
                    try
                    {
                        client.DownloadFile("https://osu.ppy.sh/osu/" + playData[i].beatmap_id, command.Path);
                    }
                    catch
                    {
                        Console.Write("Error in Downloading Beatmaps");
                    }
                }
                var workingBeatmap = new ProcessorWorkingBeatmap(command.Path);

                //Stats Calculation
                var ruleset = new OsuRuleset();
                var beatmap = workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo);

                int countmiss = (int)playData[i].countmiss;
                int count50 = (int)playData[i].count50;
                int count100 = (int)playData[i].count100;
                int count300 = (int)playData[i].count300;

                double accuracy = (double)(count50 + 2 * count100 + 6 * count300)/(6 * (countmiss + count50 + count100 + count300));
                var maxCombo = (int)playData[i].maxcombo;

                var statistics = new Dictionary<HitResult, object>()
                {
                    {HitResult.Great, count300},
                    {HitResult.Good, count100},
                    {HitResult.Meh, count50},
                    {HitResult.Miss, countmiss}
                };

                //mods are in a bitwise binary sum, with each mod given by the enum Mods
                List<Mod> mods = new List<Mod>();
                int enabled_mods = (int)playData[i].enabled_mods;

                if(((int)Mods.Hidden & enabled_mods) != 0)
                    mods.Add(new OsuModHidden());

                if(((int)Mods.Flashlight & enabled_mods) != 0)
                    mods.Add(new OsuModFlashlight());

                //no need to include Nightcore because all nightcore maps also return DoubleTime by default
                if(((int)Mods.DoubleTime & enabled_mods) != 0)
                    mods.Add(new OsuModDoubleTime());

                if(((int)Mods.HalfTime & enabled_mods) != 0)
                    mods.Add(new OsuModHalfTime());

                if(((int)Mods.HardRock & enabled_mods) != 0)
                    mods.Add(new OsuModHardRock());

                if(((int)Mods.Easy & enabled_mods) != 0)
                    mods.Add(new OsuModEasy());

                if(((int)Mods.NoFail & enabled_mods) != 0)
                    mods.Add(new OsuModNoFail());

                if(((int)Mods.SpunOut & enabled_mods) != 0)
                    mods.Add(new OsuModSpunOut());

                Mod[] finalMods = mods.ToArray();

                var scoreInfo = new ScoreInfo()
                {
                    Accuracy = accuracy,
                    MaxCombo = maxCombo,
                    Mods = finalMods,
                    Statistics = statistics

                };


                workingBeatmap.Mods.Value = finalMods;

                var categoryAttribs = new Dictionary<string, double>();
                pp[i] = ruleset.CreatePerformanceCalculator(workingBeatmap, scoreInfo).Calculate(categoryAttribs);

                command.Console.WriteLine(workingBeatmap.BeatmapInfo.ToString());

                writeAttribute("Mods", finalMods.Length > 0
                    ? finalMods.Select(m => m.Acronym).Aggregate((c, n) => $"{c}, {n}")
                    : "None");
            }
            //reorder the top 100 by public
            Array.Sort(pp);
            Array.Reverse(pp);
            double ppNet = 0;
            for (int w=0; w<100; w++)
            {
                ppNet += Math.Pow(0.95,w)*pp[w];
                writeAttribute("raw pp/weighted pp", pp[w].ToString() + " / " + (Math.Pow(0.95,w)*pp[w]).ToString());
             }
            writeAttribute("Top 100 Listed Above. Net PP", ppNet.ToString());
        }

        private void writeAttribute(string name, string value) => command.Console.WriteLine($"{name.PadRight(15)}: {value}");
        //enum for mods
        public enum Mods
        {
            None           = 0,
            NoFail         = 1,
            Easy           = 2,
            TouchDevice    = 4,
            Hidden         = 8,
            HardRock       = 16,
            SuddenDeath    = 32,
            DoubleTime     = 64,
            Relax          = 128,
            HalfTime       = 256,
            Nightcore      = 512, // Only set along with DoubleTime. i.e: NC only gives 576
            Flashlight     = 1024,
            SpunOut        = 4096,
        }
    }
}
