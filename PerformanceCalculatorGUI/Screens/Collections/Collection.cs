// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace PerformanceCalculatorGUI.Screens.Collections
{
    public class Collection
    {
        public required string FileName { get; set; }
        public required string Name { get; set; }
        public required long[] Scores { get; set; }
    }
}
