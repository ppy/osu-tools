// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Platform;
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
        private FillFlowContainer buttons;

        private ToolbarRulesetSelector rulesetSelector;

        public const float CONTROL_AREA_HEIGHT = 50;

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        public PerformanceCalculatorSceneManager()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(Storage storage)
        {
            InternalChildren = new Drawable[]
            {
                new PopoverContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new FillFlowContainer()
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
                                    new Box()
                                    {
                                        Colour = OsuColour.Gray(0.2f),
                                        RelativeSizeAxes = Axes.Both,
                                    },
                                    buttons = new FillFlowContainer
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
                                                Height = 40,
                                                Width = 80
                                            },
                                            new OsuButton
                                            {
                                                Text = "profile",
                                                Height = 40,
                                                Width = 80,
                                                Alpha = 0.1f
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
                                //Masking = true,
                                Children = new Drawable[]
                                {
                                    screens = new Container
                                    {
                                        //Colour = OsuColour.Gray(0.2f),
                                        RelativeSizeAxes = Axes.Both,
                                        Children = new Drawable[]
                                        {
                                            // ??: use screen stack instead?
                                            new SimulateScreen(),
                                            /*new ProfileScreen(),
                                            */
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            //foreach (var drawable in screens)
            //    drawable.Hide();

            //SetScreen(typeof(SetupScreen));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            rulesetSelector.Current.BindTo(ruleset);
        }
    }
}
