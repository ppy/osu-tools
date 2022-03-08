// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Threading;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Toolbar;
using osu.Game.Rulesets;
using osuTK;
using PerformanceCalculatorGUI.Screens;

namespace PerformanceCalculatorGUI
{
    public class PerformanceCalculatorSceneManager : CompositeDrawable
    {
        private Container screens;

        private ToolbarRulesetSelector rulesetSelector;

        public const float CONTROL_AREA_HEIGHT = 50;

        public const float SCREEN_SWITCH_HEIGHT = 40;
        public const float SCREEN_SWITCH_WIDTH = 100;

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        public PerformanceCalculatorSceneManager()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new PopoverContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new FillFlowContainer
                    {
                        FillMode = FillMode.Fill,
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = CONTROL_AREA_HEIGHT,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        Colour = OsuColour.Gray(0.1f),
                                        RelativeSizeAxes = Axes.Both,
                                    },
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(5),
                                        Padding = new MarginPadding(5),
                                        Children = new Drawable[]
                                        {
                                            new OsuButton
                                            {
                                                Text = "simulate",
                                                Height = SCREEN_SWITCH_HEIGHT,
                                                Width = SCREEN_SWITCH_WIDTH,
                                                Action = () => SetScreen(typeof(SimulateScreen))
                                            },
                                            new OsuButton
                                            {
                                                Text = "profile",
                                                Height = SCREEN_SWITCH_HEIGHT,
                                                Width = SCREEN_SWITCH_WIDTH,
                                                Alpha = 0.1f,
                                                Action = () => SetScreen(typeof(ProfileScreen))
                                            },
                                            new OsuButton
                                            {
                                                Text = "leaderboard",
                                                Height = SCREEN_SWITCH_HEIGHT,
                                                Width = SCREEN_SWITCH_WIDTH,
                                                Alpha = 0.1f,
                                                Action = () => SetScreen(typeof(LeaderboardScreen))
                                            },
                                            rulesetSelector = new ToolbarRulesetSelector()
                                        }
                                    },
                                },
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Y = CONTROL_AREA_HEIGHT,
                                FillMode = FillMode.Fill,
                                Anchor = Anchor.TopLeft,
                                Origin = Anchor.TopLeft,
                                Children = new Drawable[]
                                {
                                    screens = new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Children = new Drawable[]
                                        {
                                            new SimulateScreen(),
                                            new ProfileScreen(),
                                            new LeaderboardScreen()
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            foreach (var drawable in screens)
                drawable.Hide();

            SetScreen(typeof(SimulateScreen));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            rulesetSelector.Current.BindTo(ruleset);
        }

        private float depth;
        private Drawable currentScreen;
        private ScheduledDelegate scheduledHide;

        public void SetScreen(Type screenType)
        {
            var target = screens.FirstOrDefault(s => s.GetType() == screenType);

            if (target == null || currentScreen == target) return;

            if (scheduledHide?.Completed == false)
            {
                scheduledHide.RunTask();
                scheduledHide.Cancel(); // see https://github.com/ppy/osu-framework/issues/2967
                scheduledHide = null;
            }

            var lastScreen = currentScreen;
            currentScreen = target;

            lastScreen?.Hide();

            screens.ChangeChildDepth(currentScreen, depth--);
            currentScreen.Show();
        }
    }
}
