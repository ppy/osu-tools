// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Game;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Rulesets.Osu;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI
{
    public class PerformanceCalculatorGame : OsuGameBase
    {
        private LoadingSpinner loadingSpinner;

        private DependencyContainer dependencies;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) =>
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager frameworkConfig)
        {
            var windowMode = frameworkConfig.GetBindable<WindowMode>(FrameworkSetting.WindowMode);

            frameworkConfig.GetBindable<double>(FrameworkSetting.VolumeUniversal).Value = 0.1;

            var apiConfig = new SettingsManager(Storage);
            dependencies.CacheAs(apiConfig);
            dependencies.CacheAs(new APIManager(apiConfig));

            Ruleset.Value = new OsuRuleset().RulesetInfo;

            Add(loadingSpinner = new LoadingSpinner(true, true)
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Margin = new MarginPadding(40),
            });

            loadingSpinner.Show();

            var dialogOverlay = new DialogOverlay();
            dependencies.CacheAs(dialogOverlay);

            LoadComponentsAsync(new Drawable[]
            {
                new OsuContextMenuContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new PerformanceCalculatorSceneManager()
                },
                dialogOverlay
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
