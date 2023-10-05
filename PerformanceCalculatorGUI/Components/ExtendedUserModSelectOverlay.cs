// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Game.Overlays;
using osu.Game.Overlays.Mods;

namespace PerformanceCalculatorGUI.Components
{
    public partial class ExtendedUserModSelectOverlay : UserModSelectOverlay
    {
        protected override bool ShowModEffects => false;

        public ExtendedUserModSelectOverlay()
            : base(OverlayColourScheme.Blue)
        {
        }

        protected override void PopIn()
        {
            Header.Hide();
            MainAreaContent.Padding = new MarginPadding { Bottom = 64 };

            base.PopIn();
        }
    }
}
