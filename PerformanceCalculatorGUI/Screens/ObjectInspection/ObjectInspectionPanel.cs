
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    internal class ObjectInspectionPanel : Container
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
                CornerRadius = 5,
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
            textFlow.AddParagraph(text, p => p.Font = OsuFont.GetFont(size: fontSize));
        }
    }
}
