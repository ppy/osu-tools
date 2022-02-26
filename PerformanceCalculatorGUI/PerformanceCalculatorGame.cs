
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Game;
using osu.Game.Graphics;
using osu.Game.Graphics.Backgrounds;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Osu;

namespace PerformanceCalculatorGUI
{
    public class PerformanceCalculatorGame : OsuGameBase
    {
        private Bindable<WindowMode> windowMode;
        private LoadingSpinner loadingSpinner;

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager frameworkConfig)
        {
            windowMode = frameworkConfig.GetBindable<WindowMode>(FrameworkSetting.WindowMode);

            frameworkConfig.GetBindable<double>(FrameworkSetting.VolumeUniversal).Value = 0.1;

            Ruleset.Value = new OsuRuleset().RulesetInfo;

            LoadComponentAsync(new Background("Menu/menu-background-0")
            {
                Colour = OsuColour.Gray(0.5f),
                Depth = 100
            }, AddInternal);

            Add(loadingSpinner = new LoadingSpinner(true, true)
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Margin = new MarginPadding(40),
            });

            loadingSpinner.Show();

            LoadComponentsAsync(new Drawable[]
            {
                new OsuContextMenuContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new PerformanceCalculatorSceneManager()
                }
            }, drawables =>
            {
                loadingSpinner.Hide();
                loadingSpinner.Expire();

                AddRange(drawables);

                windowMode.BindValueChanged(mode => ScheduleAfterChildren(() =>
                {
                    windowMode.Value = WindowMode.Windowed;
                }), true);
            });
        }
    }
}
