// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Game;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Osu;
using PerformanceCalculatorGUI.API;

namespace PerformanceCalculatorGUI
{
    public class PerformanceCalculatorGame : OsuGameBase
    {
        private Bindable<WindowMode> windowMode;
        private LoadingSpinner loadingSpinner;

        private DependencyContainer dependencies;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) =>
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager frameworkConfig)
        {
            windowMode = frameworkConfig.GetBindable<WindowMode>(FrameworkSetting.WindowMode);

            frameworkConfig.GetBindable<double>(FrameworkSetting.VolumeUniversal).Value = 0.1;

            dependencies.CacheAs(new APIConfigManager(Storage));

            Ruleset.Value = new OsuRuleset().RulesetInfo;

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
