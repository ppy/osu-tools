// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Net;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Skinning;
using FileWebRequest = osu.Framework.IO.Network.FileWebRequest;

namespace PerformanceCalculatorGUI
{
    /// <summary>
    /// A <see cref="WorkingBeatmap"/> which reads from a .osu file.
    /// </summary>
    public class ProcessorWorkingBeatmap : WorkingBeatmap
    {
        private readonly Beatmap beatmap;
        private readonly AudioManager audioManager;

        /// <summary>
        /// Constructs a new <see cref="ProcessorWorkingBeatmap"/> from a .osu file.
        /// </summary>
        /// <param name="file">The .osu file.</param>
        /// <param name="beatmapId">An optional beatmap ID (for cases where .osu file doesn't have one).</param>
        /// <param name="audioManager"></param>
        public ProcessorWorkingBeatmap(string file, int? beatmapId = null, AudioManager audioManager = null)
            : this(readFromFile(file), beatmapId, audioManager)
        {
            this.audioManager = audioManager;
        }

        private ProcessorWorkingBeatmap(Beatmap beatmap, int? beatmapId = null, AudioManager audioManager = null)
            : base(beatmap.BeatmapInfo, audioManager)
        {
            this.beatmap = beatmap;
            this.audioManager = audioManager;

            beatmap.BeatmapInfo.Ruleset = RulesetHelper.GetRulesetFromLegacyID(beatmap.BeatmapInfo.Ruleset.OnlineID).RulesetInfo;

            if (beatmapId.HasValue)
                beatmap.BeatmapInfo.OnlineID = beatmapId.Value;
        }

        private static Beatmap readFromFile(string filename)
        {
            using (var stream = File.OpenRead(filename))
            using (var reader = new LineBufferedReader(stream))
                return Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
        }

        public static ProcessorWorkingBeatmap FromFileOrId(string fileOrId, AudioManager audioManager = null, string cachePath = "cache")
        {
            if (fileOrId.EndsWith(".osu", StringComparison.Ordinal))
            {
                if (!File.Exists(fileOrId))
                    throw new ArgumentException($"Beatmap file {fileOrId} does not exist.");

                return new ProcessorWorkingBeatmap(fileOrId, null, audioManager);
            }

            if (!int.TryParse(fileOrId, out var beatmapId))
                throw new ArgumentException("Could not parse provided beatmap ID.");

            cachePath = Path.Combine(cachePath, $"{beatmapId}.osu");

            if (!File.Exists(cachePath))
            {
                Console.WriteLine($"Downloading {beatmapId}.osu...");

                try
                {
                    new FileWebRequest(cachePath, $"{APIManager.ENDPOINT_CONFIGURATION.WebsiteRootUrl}/osu/{beatmapId}").Perform();
                }
                catch (WebException)
                {
                    // FileWebRequest will sometimes create a file regardless of status
                    if (File.Exists(cachePath))
                        File.Delete(cachePath);

                    throw;
                }

                // FileWebRequest will always create an empty file if the beatmap doesn't exist, clean it up
                if (new System.IO.FileInfo(cachePath).Length == 0)
                    File.Delete(cachePath);

                if (!File.Exists(cachePath))
                    throw new ArgumentException($"Beatmap {beatmapId} does not exist.");
            }

            try
            {
                return new ProcessorWorkingBeatmap(readFromFile(cachePath), beatmapId, audioManager);
            }
            catch (Exception)
            {
                // remove maps that failed to import - its safer to try redownloading it later than keeping a broken map
                File.Delete(cachePath);
                throw;
            }
        }

        protected override Track GetBeatmapTrack()
        {
            const double excess_length = 1000;

            var lastObject = Beatmap?.HitObjects.LastOrDefault();

            double length;

            switch (lastObject)
            {
                case null:
                    length = 1000;
                    break;

                case IHasDuration endTime:
                    length = endTime.EndTime + excess_length;
                    break;

                default:
                    length = lastObject.StartTime + excess_length;
                    break;
            }

            return audioManager.Tracks.GetVirtual(length);
        }

        protected override IBeatmap GetBeatmap() => beatmap;
        public override Texture GetBackground() => null;
        protected override ISkin GetSkin() => null;
        public override Stream GetStream(string storagePath) => null;
    }
}
