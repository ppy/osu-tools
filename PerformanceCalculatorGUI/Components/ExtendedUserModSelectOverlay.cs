// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Game.Overlays.Mods;

namespace PerformanceCalculatorGUI.Components
{
    internal class ExtendedUserModSelectOverlay : UserModSelectOverlay
    {
        private const float animation_duration = 100;

        protected override void PopIn()
        {
            Schedule(() => GetContainingInputManager().TriggerFocusContention(this));

            Waves.Show(); // we have to show waves once 
            this.FadeIn(animation_duration);
        }

        protected override void PopOut()
        {
            if (!HasFocus)
                return;

            GetContainingInputManager().ChangeFocus(null);

            this.FadeOut(animation_duration);
        }
    }
}
