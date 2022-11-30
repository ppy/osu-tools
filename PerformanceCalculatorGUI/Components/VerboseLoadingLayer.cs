// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osuTK.Graphics;

namespace PerformanceCalculatorGUI.Components
{
    /// <summary>
    /// A <see cref="LoadingLayer"/> with additional text displayed below the spinner.
    /// </summary>
    public partial class VerboseLoadingLayer : LoadingLayer
    {
        public Bindable<string> Text = new Bindable<string>();

        public VerboseLoadingLayer(bool dimBackground = false, bool withBox = true)
            : base(dimBackground, withBox)
        {
            AddInternal(new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 5f,
                Y = 75,
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.Black,
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0.7f
                    },
                    new OsuSpriteText
                    {
                        Padding = new MarginPadding(5f),
                        Current = { BindTarget = Text }
                    }
                }
            });
        }
    }
}
