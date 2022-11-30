// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Toolbar;
using osu.Game.Rulesets;
using osuTK;
using osuTK.Graphics;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Screens;

namespace PerformanceCalculatorGUI
{
    public partial class PerformanceCalculatorSceneManager : CompositeDrawable
    {
        private ScreenStack screenStack;

        private ToolbarRulesetSelector rulesetSelector;

        private Box hoverGradientBox;

        public const float CONTROL_AREA_HEIGHT = 45;

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        [Resolved]
        private DialogOverlay dialogOverlay { get; set; }

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
                                new HoverHandlingContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = CONTROL_AREA_HEIGHT,
                                    Hovered = e =>
                                    {
                                        hoverGradientBox.FadeIn(100);
                                        return false;
                                    },
                                    Unhovered = e =>
                                    {
                                        hoverGradientBox.FadeOut(100);
                                    },
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
                                new ScalingContainer(ScalingMode.Everything)
                                {
                                    Depth = 1,
                                    Children = new Drawable[]
                                    {
                                        screenStack = new ScreenStack
                                        {
                                            RelativeSizeAxes = Axes.Both
                                        },
                                        hoverGradientBox = new Box
                                        {
                                            Colour = ColourInfo.GradientVertical(Color4.Black.Opacity(1.0f), Color4.Black.Opacity(1)),
                                            RelativeSizeAxes = Axes.X,
                                            Height = 100,
                                            Alpha = 0
                                        }
                                    }
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
