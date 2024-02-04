// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osuTK;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class ObjectInspectorDifficultyValue : GridContainer
    {
        private readonly string label;
        private readonly string value;

        public ObjectInspectorDifficultyValue(string label, double value)
        {
            this.label = label;
            this.value = value.ToString("N2");

            createLayout();
        }

        public ObjectInspectorDifficultyValue(string label, Vector2 value)
        {
            this.label = label;
            this.value = $"({value.X:N2}; {value.Y:N2})";

            createLayout();
        }

        private void createLayout()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            ColumnDimensions = new[]
            {
                new Dimension(GridSizeMode.AutoSize),
                new Dimension()
            };
            RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) };
            Content = new[]
            {
                new Drawable[]
                {
                    new SpriteText
                    {
                        Text = label,
                        Font = OsuFont.GetFont(weight: FontWeight.SemiBold)
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        Text = value,
                        Font = OsuFont.GetFont(weight: FontWeight.Regular, fixedWidth: true)
                    }
                }
            };
        }
    }
}
