// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using System.Collections.Generic;
using System.IO;
using osu.Game.IO.Archives;

namespace PerformanceCalculator
{
    public class SingleFileArchiveReader : ArchiveReader
    {
        private readonly string file;

        public SingleFileArchiveReader(string file)
            : base(Path.GetFileName(file))
        {
            this.file = file;
        }

        public override Stream GetStream(string name) => File.OpenRead(file);

        public override void Dispose()
        {
        }

        public override IEnumerable<string> Filenames => new[] { file };
        
        public override Stream GetUnderlyingStream() => null;
    }
}
