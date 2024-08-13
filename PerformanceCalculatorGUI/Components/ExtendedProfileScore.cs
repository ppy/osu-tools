// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;

namespace PerformanceCalculatorGUI.Components
{
    public class ExtendedProfileScore : ProfileScore
    {
        public double LivePP { get; }
        public Bindable<int> PositionChange { get; } = new Bindable<int>();

        public ExtendedProfileScore(SoloScoreInfo score, double livePP, PerformanceAttributes attributes)
            : base(score, attributes)
        {
            LivePP = livePP;
        }
    }

    public partial class DrawableExtendedProfileScore : DrawableProfileScore
    {
        protected new ExtendedProfileScore Score { get; }

        public DrawableExtendedProfileScore(ExtendedProfileScore score)
            : base(score)
        {
            Score = score;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Score.Position.UnbindEvents();
            Score.PositionChange.BindValueChanged(v => { PositionText.Text = $"{v.NewValue:+0;-0;-}"; });
        }

        protected override Drawable[] CreateRightInfoContainerContent(RulesetStore rulesets)
        {
            return new Drawable[]
            {
                new FillFlowContainer
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Width = 60,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            AutoSizeAxes = Axes.Y,
                            Child = new OsuSpriteText
                            {
                                Font = OsuFont.GetFont(weight: FontWeight.Bold),
                                Text = $"{Score.LivePP:0}pp"
                            },
                        },
                        new OsuSpriteText
                        {
                            Font = OsuFont.GetFont(size: SMALL_TEXT_FONT_SIZE),
                            Text = "live"
                        }
                    }
                }
            }.Concat(base.CreateRightInfoContainerContent(rulesets)).ToArray();
        }

        protected override Drawable CreatePerformanceInfo()
        {
            return new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Padding = new MarginPadding
                {
                    Vertical = 5,
                    Left = 30,
                    Right = 20
                },
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    new ExtendedOsuSpriteText
                    {
                        Font = OsuFont.GetFont(weight: FontWeight.Bold),
                        Text = $"{Score.SoloScore.PP:0}pp",
                        Colour = ColourProvider.Highlight1,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        TooltipContent = $"{AttributeConversion.ToReadableString(Score.PerformanceAttributes)}"
                    },
                    new OsuSpriteText
                    {
                        Font = OsuFont.GetFont(size: SMALL_TEXT_FONT_SIZE),
                        Text = $"{Score.SoloScore.PP - Score.LivePP:+0.0;-0.0;-}",
                        Colour = ColourProvider.Light1,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre
                    }
                }
            };
        }
    }
}
