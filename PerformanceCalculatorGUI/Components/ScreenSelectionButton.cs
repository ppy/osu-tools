// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osuTK;
using osuTK.Graphics;

namespace PerformanceCalculatorGUI.Components
{
    public partial class ScreenSelectionButton : OsuClickableContainer, IKeyBindingHandler<GlobalAction>, IHasCustomTooltip<(string, GlobalAction?)>
    {
        private readonly string title;
        private readonly GlobalAction? hotkey;

        private const float padding = 3;

        private readonly Box hoverBackground;
        private readonly Box flashBackground;

        public ScreenSelectionButton(string title, IconUsage? icon = null, GlobalAction? hotkey = null)
        {
            this.title = title;
            this.hotkey = hotkey;

            AutoSizeAxes = Axes.X;
            RelativeSizeAxes = Axes.Y;

            Children = new Drawable[]
            {
                new Container
                {
                    Width = PerformanceCalculatorSceneManager.CONTROL_AREA_HEIGHT,
                    RelativeSizeAxes = Axes.Y,
                    Padding = new MarginPadding(padding),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            CornerRadius = 6,
                            CornerExponent = 3f,
                            Children = new Drawable[]
                            {
                                hoverBackground = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = OsuColour.Gray(80).Opacity(180),
                                    Blending = BlendingParameters.Additive,
                                    Alpha = 0
                                },
                                flashBackground = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Alpha = 0,
                                    Colour = Color4.White.Opacity(100),
                                    Blending = BlendingParameters.Additive
                                }
                            }
                        },
                        new FillFlowContainer
                        {
                            Direction = FillDirection.Horizontal,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Padding = new MarginPadding { Horizontal = PerformanceCalculatorSceneManager.CONTROL_AREA_HEIGHT / 2 },
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                            Children = new Drawable[]
                            {
                                new ConstrainedIconContainer
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(25),
                                    Icon = new ScreenSelectionButtonIcon(icon) { IconSize = new Vector2(20) }
                                }
                            }
                        }
                    }
                }
            };
        }

        protected override bool OnMouseDown(MouseDownEvent e) => false;

        protected override bool OnClick(ClickEvent e)
        {
            flashBackground.FadeIn(50).Then().FadeOutFromOne(800, Easing.OutQuint);
            return base.OnClick(e);
        }

        protected override bool OnHover(HoverEvent e)
        {
            hoverBackground.FadeIn(300, Easing.OutQuint);
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            hoverBackground.FadeOut(200, Easing.Out);
        }

        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Action == hotkey && !e.Repeat)
            {
                TriggerClick();
                return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        public ITooltip<(string, GlobalAction?)> GetCustomTooltip()
        {
            return new ScreenSelectionButtonTooltip();
        }

        public (string, GlobalAction?) TooltipContent => (title, hotkey);

        public partial class ScreenSelectionButtonTooltip : VisibilityContainer, ITooltip<(string, GlobalAction?)>
        {
            private (string, GlobalAction?)? currentData;

            private readonly FillFlowContainer subTooltipFlow;
            private readonly OsuSpriteText text;

            public ScreenSelectionButtonTooltip()
            {
                AutoSizeAxes = Axes.Both;
                Masking = true;
                CornerRadius = 5;

                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Shadow,
                    Colour = Color4.Black.Opacity(0.2f),
                    Radius = 10f
                };

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(0.1f)
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Padding = new MarginPadding(8f),
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(5),
                        Children = new Drawable[]
                        {
                            text = new OsuSpriteText
                            {
                                Anchor = Anchor.TopLeft,
                                Origin = Anchor.TopLeft,
                                Shadow = true,
                                Font = OsuFont.GetFont(size: 18, weight: FontWeight.Bold)
                            },
                            subTooltipFlow = new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Anchor = Anchor.TopLeft,
                                Origin = Anchor.TopLeft,
                                Direction = FillDirection.Horizontal
                            }
                        }
                    }
                };
            }

            protected override void PopIn() => this.FadeIn(300, Easing.OutQuint);
            protected override void PopOut() => this.FadeOut(200, Easing.Out);

            public void SetContent((string, GlobalAction?) data)
            {
                if (currentData == data)
                    return;

                currentData = data;
                subTooltipFlow.Clear();

                text.Text = data.Item1;

                if (data.Item2 != null)
                {
                    subTooltipFlow.Add(new HotkeyDisplay
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Hotkey = new Hotkey(data.Item2.Value),
                        Margin = new MarginPadding { Left = 3 }
                    });
                }
            }

            public void Move(Vector2 pos) => Position = pos;
        }
    }
}
