// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Sprites;

namespace PerformanceCalculatorGUI.Screens
{
    internal class ProfileScreen : CompositeDrawable
    {
        public ProfileScreen()
        {
            RelativeSizeAxes = Axes.Both;

            FillMode = FillMode.Fill;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new OsuSpriteText
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = "Nothing to see here (yet)"
                },
            };
        }
    }
}
