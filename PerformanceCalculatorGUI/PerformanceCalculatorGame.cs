// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Game;
using osu.Game.Graphics.Cursor;
using osu.Game.Overlays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI
{
    public partial class PerformanceCalculatorGame : OsuGameBase
    {
        private Bindable<WindowMode> windowMode;
        private DependencyContainer dependencies;

        // This overwrites OsuGameBase's SelectedMods to make sure it can't tweak mods when we don't want it to
        [Cached]
        [Cached(typeof(IBindable<IReadOnlyList<Mod>>))]
        private readonly Bindable<IReadOnlyList<Mod>> mods = new Bindable<IReadOnlyList<Mod>>(Array.Empty<Mod>());

        [Resolved]
        private FrameworkConfigManager frameworkConfig { get; set; }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) =>
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        protected override IDictionary<FrameworkSetting, object> GetFrameworkConfigDefaults() => new Dictionary<FrameworkSetting, object>
        {
            { FrameworkSetting.VolumeUniversal, 0.0d },
        };

        [BackgroundDependencyLoader]
        private void load()
        {
            var apiConfig = new SettingsManager(Storage);
            dependencies.CacheAs(apiConfig);
            dependencies.CacheAs(new APIManager(apiConfig));

            Ruleset.Value = new OsuRuleset().RulesetInfo;

            var dialogOverlay = new DialogOverlay();
            dependencies.CacheAs(dialogOverlay);

            var notificationDisplay = new NotificationDisplay();
            dependencies.CacheAs(notificationDisplay);

            AddRange(new Drawable[]
            {
                new OsuContextMenuContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new PerformanceCalculatorSceneManager()
                },
                dialogOverlay,
                notificationDisplay
            });
        }

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);

            host.Window.CursorState |= CursorState.Hidden;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            windowMode = frameworkConfig.GetBindable<WindowMode>(FrameworkSetting.WindowMode);
            windowMode.BindValueChanged(mode => windowMode.Value = WindowMode.Windowed, true);
        }
    }
}
