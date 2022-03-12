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
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Toolbar;
using osu.Game.Rulesets;
using osuTK;
using PerformanceCalculatorGUI.Components;
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

        [Resolved(canBeNull: true)]
        private DialogOverlay dialogOverlay { get; set; }

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
                    Child = new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        ColumnDimensions = new[] { new Dimension() },
                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize), new Dimension() },
                        Content = new[]
                        {
                            new Drawable[]
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
                                            RelativeSizeAxes = Axes.Y,
                                            Direction = FillDirection.Horizontal,
                                            Spacing = new Vector2(5),
                                            Padding = new MarginPadding(5),
                                            AutoSizeAxes = Axes.X,
                                            Children = new Drawable[]
                                            {
                                                new OsuButton
                                                {
                                                    Text = "simulate",
                                                    Height = SCREEN_SWITCH_HEIGHT,
                                                    Width = SCREEN_SWITCH_WIDTH,
                                                    Action = () => trySettingScreen(typeof(SimulateScreen))
                                                },
                                                new OsuButton
                                                {
                                                    Text = "profile",
                                                    Height = SCREEN_SWITCH_HEIGHT,
                                                    Width = SCREEN_SWITCH_WIDTH,
                                                    Action = () => trySettingScreen(typeof(ProfileScreen))
                                                },
                                                new OsuButton
                                                {
                                                    Text = "leaderboard",
                                                    Height = SCREEN_SWITCH_HEIGHT,
                                                    Width = SCREEN_SWITCH_WIDTH,
                                                    Action = () => trySettingScreen(typeof(LeaderboardScreen))
                                                }
                                            }
                                        },
                                        new FillFlowContainer
                                        {
                                            Anchor = Anchor.TopRight,
                                            Origin = Anchor.TopRight,
                                            Direction = FillDirection.Horizontal,
                                            RelativeSizeAxes = Axes.Y,
                                            AutoSizeAxes = Axes.X,
                                            Spacing = new Vector2(5),
                                            Children = new Drawable[]
                                            {
                                                rulesetSelector = new ToolbarRulesetSelector(),
                                                new SettingsButton()
                                            }
                                        },
                                    },
                                }
                            },
                            new Drawable[]
                            {
                                screens = new Container
                                {
                                    Depth = 1,
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
            };

            foreach (var drawable in screens)
                drawable.Hide();

            setScreen(typeof(SimulateScreen));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            rulesetSelector.Current.BindTo(ruleset);
        }

        private float depth;
        private Drawable currentScreen;
        private ScheduledDelegate scheduledHide;

        private void trySettingScreen(Type screenType)
        {
            if (currentScreen is PerformanceCalculatorScreen screen)
            {
                if (screen.ShouldShowConfirmationDialogOnSwitch)
                {
                    dialogOverlay.Push(new ConfirmDialog("Are you sure?", () =>
                    {
                        setScreen(screenType);
                    }));
                }
                else
                {
                    setScreen(screenType);
                }
            }
        }

        private void setScreen(Type screenType)
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
