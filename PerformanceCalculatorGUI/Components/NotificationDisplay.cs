
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace PerformanceCalculatorGUI.Components
{
    internal class NotificationDisplay : Container
    {
        private readonly FillFlowContainer content;

        public NotificationDisplay()
        {
            RelativeSizeAxes = Axes.Both;

            Children = new Drawable[]
            {
                content = new FillFlowContainer
                {
                    Direction = FillDirection.Vertical,
                    Origin = Anchor.TopRight,
                    Anchor = Anchor.TopRight,
                    RelativeSizeAxes = Axes.Y,
                    Height = 1,
                    Width = 350,
                    Padding = new MarginPadding(20),
                    Spacing = new Vector2(0, 10)
                }
            };
        }

        public void Display(Notification notification) => Schedule(() =>
        {
            content.Add(notification);

            notification.FadeIn(1500, Easing.OutQuint)
                        .Delay(5000)
                        .FadeOut(1500, Easing.OutQuint)
                        .Finally(_ => content.Remove(notification));
        });
    }
}
