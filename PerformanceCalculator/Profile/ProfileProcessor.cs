// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
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
            //initializing information-holding sorted list
            var sortedPP = new SortedDictionary<double,PPInfo>();
            //initialize path to download beatmap files
            string path = command.Path + @"\ProfileCalculator.txt";

            //get data for all 100 top plays
            var getPlayData = (HttpWebRequest) WebRequest.Create("https://osu.ppy.sh/api/get_user_best?k="+command.Key+"&u="+command.ProfileName+"&limit=100&type=username");
            HttpWebResponse response = (HttpWebResponse)getPlayData.GetResponse();
            var receiveStream = response.GetResponseStream();
            var readStream = new StreamReader(receiveStream);
            string json = readStream.ReadToEnd();
            var playData = JsonConvert.DeserializeObject<dynamic>(json);
            response.Close();
            receiveStream.Close();
            readStream.Close();

            //create the file if it doesnt exist
            if(!File.Exists(path))
            {
                using(File.Create(path)) ;
            }

            for(int i=0; i<100; i++)
            {
                //for each beatmap, download it
                using (var client = new WebClient()) {
                    try
                    {
                        client.DownloadFile("https://osu.ppy.sh/osu/" + playData[i].beatmap_id, path);
                    }
                    catch
                    {
                        Console.WriteLine("Error in Downloading Beatmaps");
                    }
                }

                var workingBeatmap = new ProcessorWorkingBeatmap(path);

                //Stats Calculation
                var ruleset = new OsuRuleset();

                double countmiss = playData[i].countmiss;
                double count50 = playData[i].count50;
                double count100 = playData[i].count100;
                double count300 = playData[i].count300;

                double accuracy = (count50 + 2 * count100 + 6 * count300) / (6 * (countmiss + count50 + count100 + count300));
                var maxCombo = (int)playData[i].maxcombo;

                var statistics = new Dictionary<HitResult, object>
                {
                    {HitResult.Great, (int)count300},
                    {HitResult.Good, (int)count100},
                    {HitResult.Meh, (int)count50},
                    {HitResult.Miss, (int)countmiss}
                };

                //mods are in a bitwise binary sum, with each mod given by the enum Mods
                List<Mod> mods = new List<Mod>();
                int enabledMods = (int)playData[i].enabled_mods;

                if(((int)Mods.Hidden & enabledMods) != 0)
                    mods.Add(new OsuModHidden());

                if(((int)Mods.Flashlight & enabledMods) != 0)
                    mods.Add(new OsuModFlashlight());

                //no need to include Nightcore because all nightcore maps also return DoubleTime by default
                if(((int)Mods.DoubleTime & enabledMods) != 0)
                    mods.Add(new OsuModDoubleTime());

                if(((int)Mods.HalfTime & enabledMods) != 0)
                    mods.Add(new OsuModHalfTime());

                if(((int)Mods.HardRock & enabledMods) != 0)
                    mods.Add(new OsuModHardRock());

                if(((int)Mods.Easy & enabledMods) != 0)
                    mods.Add(new OsuModEasy());

                if(((int)Mods.NoFail & enabledMods) != 0)
                    mods.Add(new OsuModNoFail());

                if(((int)Mods.SpunOut & enabledMods) != 0)
                    mods.Add(new OsuModSpunOut());

                Mod[] finalMods = mods.ToArray();

                var scoreInfo = new ScoreInfo
                {
                    Accuracy = accuracy,
                    MaxCombo = maxCombo,
                    Mods = finalMods,
                    Statistics = statistics
                };

                workingBeatmap.Mods.Value = finalMods;

                var categoryAttribs = new Dictionary<string, double>();
                double pp = ruleset.CreatePerformanceCalculator(workingBeatmap, scoreInfo).Calculate(categoryAttribs);
                var outputInfo = new PPInfo
                {
                    OldPP = (double)playData[i].pp,
                    BeatmapInfo = workingBeatmap.BeatmapInfo.ToString(),
                    ModInfo = finalMods.Length > 0
                    ? finalMods.Select(m => m.Acronym).Aggregate((c, n) => $"{c}, {n}")
                    : "None"
                };
                sortedPP.Add(pp, outputInfo);
            }

            double oldPPNet = 0;
            double ppNet = 0;
            int w = 0;
            foreach(KeyValuePair<double,PPInfo> kvp in sortedPP.Reverse())
            {
                ppNet += Math.Pow(0.95,w)*kvp.Key;
                oldPPNet += Math.Pow(0.95,w)*kvp.Value.OldPP;

                writeAttribute(w+1 + ".Beatmap", kvp.Value.BeatmapInfo);
                writeAttribute("Mods", kvp.Value.ModInfo);
                writeAttribute("old/new pp", kvp.Value.OldPP.ToString(CultureInfo.InvariantCulture) + " / " + kvp.Key.ToString(CultureInfo.InvariantCulture));
                w++;
            }

            if(command.Bonus == 1)
            {
                //get user data (used for bonus pp calculation)
                var getUserData = (HttpWebRequest) WebRequest.Create("https://osu.ppy.sh/api/get_user?k="+command.Key+"&u="+command.ProfileName+"&type=username");
                response = (HttpWebResponse)getUserData.GetResponse();
                receiveStream = response.GetResponseStream();
                readStream = new StreamReader(receiveStream);
                json = readStream.ReadToEnd();
                var userData = JsonConvert.DeserializeObject<dynamic>(json);
                response.Close();
                receiveStream.Close();
                readStream.Close();

                double bonusPP = 0;
                //inactive players have 0pp to take them out of the leaderboard
                if(userData[0].pp_raw == 0)
                    command.Console.WriteLine("The player has 0 pp or is inactive, so bonus pp cannot be calculated");
                //calculate bonus pp as difference of user pp and sum of other pps
                else
                {
                    bonusPP = userData[0].pp_raw - oldPPNet;
                    oldPPNet = userData[0].pp_raw;
                }
                //add on bonus pp
                ppNet += bonusPP;
            }
            writeAttribute("Top 100 Listed Above. Old/New Net PP", oldPPNet.ToString(CultureInfo.InvariantCulture) + " / " + ppNet.ToString(CultureInfo.InvariantCulture));
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
