// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK.Graphics;

namespace PerformanceCalculatorGUI.Components
{
    public partial class Notification : Container
    {
        public Notification(LocalisableString text)
        {
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
            Alpha = 0;
            Masking = true;
            CornerRadius = 10;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.DarkSlateGray,
                    Alpha = 0.95f
                },
                new OsuSpriteText
                {
                    Padding = new MarginPadding(10),
                    Name = "Description",
                    AllowMultiline = true,
                    RelativeSizeAxes = Axes.X,
                    Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold),
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Text = text
                }
            };
        }
    }
}
