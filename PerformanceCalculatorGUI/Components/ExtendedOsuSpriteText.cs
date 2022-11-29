// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace PerformanceCalculatorGUI.Components
{
    public partial class ExtendedOsuSpriteText : OsuSpriteText, IHasCustomTooltip<string>
    {
        public override bool HandlePositionalInput => true;

        public string TooltipContent { get; set; }

        public ITooltip<string> GetCustomTooltip() => new MultilineTooltip();
    }

    public partial class MultilineTooltip : VisibilityContainer, ITooltip<string>
    {
        private readonly FillFlowContainer textContainer;
        private string currentData;

        public MultilineTooltip()
        {
            AutoSizeAxes = Axes.Both;
            Masking = true;
            CornerRadius = 5;

            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0.85f,
                    Colour = OsuColour.Gray(0.1f)
                },
                textContainer = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding(10),
                },
            };
        }

        protected override void PopIn() => this.FadeIn(200, Easing.OutQuint);
        protected override void PopOut() => this.FadeOut(200, Easing.OutQuint);

        public void SetContent(string data)
        {
            if (currentData == data)
                return;

            textContainer.Clear();

            currentData = data;

            var split = data.Split('\n');

            foreach (var line in split)
                textContainer.Add(new OsuSpriteText { Text = line });
        }

        public void Move(Vector2 pos) => Position = pos;
    }
}
