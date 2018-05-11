// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Scoring;

namespace PerformanceCalculator
{
    public class Calculator : Component
    {
        private readonly string beatmapFile;
        private readonly string[] replayFiles;
        private readonly IConsole console;

        public Calculator(string beatmapFile, string[] replayFiles, IConsole console)
        {
            this.beatmapFile = beatmapFile;
            this.replayFiles = replayFiles;
            this.console = console;
        }

        private WorkingBeatmap workingBeatmap;
        private Ruleset ruleset;

        [BackgroundDependencyLoader]
        private void load(BeatmapManager beatmaps, ScoreStore scores, GameHost host)
        {
            if (workingBeatmap == null)
                beatmaps.Import(new SingleFileArchiveReader(beatmapFile));

            foreach (var f in replayFiles)
            {
                var score = scores.ReadReplayFile(f);

                if (ruleset == null)
                    ruleset = score.Ruleset.CreateInstance();

                // Create beatmap
                if (workingBeatmap == null)
                    workingBeatmap = beatmaps.GetWorkingBeatmap(score.Beatmap);
                workingBeatmap.Mods.Value = score.Mods;

                // Convert + process beatmap
                IBeatmap converted = ruleset.CreateBeatmapConverter(workingBeatmap.GetPlayableBeatmap(score.Ruleset)).Convert();
                ruleset.CreateBeatmapProcessor(converted).PostProcess();

                var categoryAttribs = new Dictionary<string, double>();
                double pp = ruleset.CreatePerformanceCalculator(converted, score).Calculate(categoryAttribs);
                
                console.Out.WriteLine(f);
                foreach (var kvp in categoryAttribs)
                    console.Out.WriteLine($"{kvp.Key.PadRight(15)}: {kvp.Value}");
                console.Out.WriteLine($"{"pp".PadRight(15)}: {pp}");
            }
            
            host.Exit();
        }
    }
}
