// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using PerformanceCalculatorGUI.Components.TextBoxes;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class ObjectInspectionPanel : Container
    {
        private OsuTextFlowContainer textFlow;

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(new Container
            {
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = ExtendedLabelledTextBox.CORNER_RADIUS,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0.95f,
                        Colour = OsuColour.Gray(0.1f)
                    },
                    textFlow = new OsuTextFlowContainer
                    {
                        Padding = new MarginPadding(5f),
                        AutoSizeAxes = Axes.Both,
                    }
                }
            });
        }

        public void AddParagraph(string text, int fontSize = 10)
        {
            textFlow.AddParagraph(text, p => p.Font = OsuFont.GetFont(size: fontSize - 3));
        }
    }
}
