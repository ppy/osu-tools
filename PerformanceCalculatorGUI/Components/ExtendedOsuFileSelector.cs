// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.IO;
using FileInfo = System.IO.FileInfo;

namespace PerformanceCalculatorGUI.Components
{
    public class LegacyBeatmapMetadataDecoder : LegacyBeatmapDecoder
    {
        protected override void ParseLine(Beatmap beatmap, Section section, string line)
        {
            // early out to only parse relevant data
            if (section != Section.Metadata && section != Section.General)
                return;

            base.ParseLine(beatmap, section, line);
        }
    }

    public partial class ExtendedOsuFileSelector : OsuFileSelector
    {
        public ExtendedOsuFileSelector(string initialPath = null, string[] validFileExtensions = null)
            : base(initialPath, validFileExtensions)
        {
        }

        /// <summary>
        /// Metadata decoder is created once to not recreate it for every file
        /// </summary>
        private readonly LegacyBeatmapMetadataDecoder beatmapDecoder = new LegacyBeatmapMetadataDecoder();

        protected override DirectoryListingFile CreateFileItem(FileInfo file) => new ExtendedOsuDirectoryListingFile(file, beatmapDecoder);

        protected partial class ExtendedOsuDirectoryListingFile : OsuDirectoryListingFile
        {
            private readonly LegacyBeatmapMetadataDecoder beatmapDecoder;

            protected override string FallbackName
            {
                get
                {
                    var beatmap = beatmapDecoder.Decode(new LineBufferedReader(File.OpenRead()));
                    return $"{File.Name} | [{beatmap.BeatmapInfo.Ruleset.Name}] {beatmap.Metadata} [{beatmap.BeatmapInfo.DifficultyName}]";
                }
            }

            public ExtendedOsuDirectoryListingFile(FileInfo file, LegacyBeatmapMetadataDecoder beatmapDecoder)
                : base(file)
            {
                this.beatmapDecoder = beatmapDecoder;
            }

            protected override IconUsage? Icon => FontAwesome.Regular.FileAlt;
        }
    }
}
