// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Screens;
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
        private ScreenStack screenStack;

        private ToolbarRulesetSelector rulesetSelector;

        public const float CONTROL_AREA_HEIGHT = 50;

        public const float SCREEN_SWITCH_HEIGHT = 40;
        public const float SCREEN_SWITCH_WIDTH = 100;

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        [Resolved]
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
                                                    Action = () => setScreen(new SimulateScreen())
                                                },
                                                new OsuButton
                                                {
                                                    Text = "profile",
                                                    Height = SCREEN_SWITCH_HEIGHT,
                                                    Width = SCREEN_SWITCH_WIDTH,
                                                    Action = () => setScreen(new ProfileScreen())
                                                },
                                                new OsuButton
                                                {
                                                    Text = "leaderboard",
                                                    Height = SCREEN_SWITCH_HEIGHT,
                                                    Width = SCREEN_SWITCH_WIDTH,
                                                    Action = () => setScreen(new LeaderboardScreen())
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
                                screenStack = new ScreenStack
                                {
                                    Depth = 1,
                                    RelativeSizeAxes = Axes.Both
                                }
                            }
                        } 
                    }
                }
            };

            setScreen(new SimulateScreen());
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            rulesetSelector.Current.BindTo(ruleset);
        }

        private void setScreen(Screen screen)
        {
            if (screenStack.CurrentScreen != null)
            {
                if (screenStack.CurrentScreen is PerformanceCalculatorScreen { ShouldShowConfirmationDialogOnSwitch: true })
                {
                    dialogOverlay.Push(new ConfirmDialog("Are you sure?", () =>
                    {
                        screenStack.Exit();
                        screenStack.Push(screen);
                    }));
                    return;
                }

                screenStack.Exit();
            }

            screenStack.Push(screen);
        }
    }
}
