// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;

namespace PerformanceCalculatorGUI.Components
{
    public partial class ExtendedOsuCheckbox : OsuCheckbox
    {
        public ColourInfo TextColour { get; set; }

        protected override void ApplyLabelParameters(SpriteText text)
        {
            base.ApplyLabelParameters(text);
            text.Colour = TextColour;
        }
    }
}
