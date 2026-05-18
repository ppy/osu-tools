// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Toolbar;
using osu.Game.Rulesets;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Screens;

namespace PerformanceCalculatorGUI
{
    [Cached]
    public partial class PerformanceCalculatorSceneManager : CompositeDrawable
    {
        private ScreenStack screenStack = null!;

        private ToolbarRulesetSelector rulesetSelector = null!;

        public const float CONTROL_AREA_HEIGHT = 45;

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private DialogOverlay dialogOverlay { get; set; } = null!;

        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

        public PerformanceCalculatorSceneManager()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
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
                                            AutoSizeAxes = Axes.X,
                                            Children = new Drawable[]
                                            {
                                                new ScreenSelectionButton("Beatmap", FontAwesome.Solid.Music)
                                                {
                                                    Action = () => setScreen(new SimulateScreen())
                                                },
                                                new ScreenSelectionButton("Profile", FontAwesome.Solid.User)
                                                {
                                                    Action = () => setScreen(new ProfileScreen())
                                                },
                                                new ScreenSelectionButton("Player Leaderboard", FontAwesome.Solid.List)
                                                {
                                                    Action = () => setScreen(new LeaderboardScreen())
                                                },
                                                new ScreenSelectionButton("Beatmap Leaderboard", FontAwesome.Solid.ListAlt)
                                                {
                                                    Action = () => setScreen(new BeatmapLeaderboardScreen())
                                                },
                                                new ScreenSelectionButton("Collections", FontAwesome.Solid.BoxOpen)
                                                {
                                                    Action = () => setScreen(new CollectionsScreen())
                                                },
                                            }
                                        },
                                        new FillFlowContainer
                                        {
                                            Anchor = Anchor.TopRight,
                                            Origin = Anchor.TopRight,
                                            Direction = FillDirection.Horizontal,
                                            RelativeSizeAxes = Axes.Y,
                                            AutoSizeAxes = Axes.X,
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
                                new ScalingContainer(ScalingMode.Everything)
                                {
                                    Child = screenStack = new ScreenStack
                                    {
                                        RelativeSizeAxes = Axes.Both
                                    },
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

        public void SwitchToSimulate(int beatmapId, ulong? scoreId = null)
        {
            setScreen(new SimulateScreen(beatmapId, scoreId));
        }

        public void SwitchToBeatmapLeaderboard(int beatmapId)
        {
            setScreen(new BeatmapLeaderboardScreen(beatmapId));
        }
    }
}
