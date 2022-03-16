// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.Profile.Sections.Ranks;
using osuTK;

namespace PerformanceCalculatorGUI.Components
{
    public class ExtendedScore : APIScore
    {
        public double LivePP { get; }

        public Bindable<int> PositionChange { get; } = new();

        public ExtendedScore(APIScore score, double livePP)
        {
            LivePP = livePP;

            TotalScore = score.TotalScore;
            MaxCombo = score.MaxCombo;
            User = score.User;
            OnlineID = score.OnlineID;
            HasReplay = score.HasReplay;
            Date = score.Date;
            Beatmap = score.Beatmap;
            Accuracy = score.Accuracy;
            PP = score.PP;
            Statistics = score.Statistics;
            RulesetID = score.RulesetID;
            Mods = score.Mods;
            Rank = score.Rank;
        }
    }

    internal class ExtendedProfileScore : DrawableProfileScore
    {
        protected readonly ExtendedScore ExtendedScore;

        private OsuSpriteText positionChangeText;

        public ExtendedProfileScore(ExtendedScore score)
            : base(score)
        {
            ExtendedScore = score;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(new Container
            {
                RelativeSizeAxes = Axes.Y,
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Width = 25,
                Child = positionChangeText = new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = ExtendedScore.PositionChange.Value.ToString()
                }
            });

            ExtendedScore.PositionChange.BindValueChanged(v => { positionChangeText.Text = $"{v.NewValue:+0;-0;-}"; });
        }

        protected override Drawable CreateRightContent() => new FillFlowContainer
        {
            AutoSizeAxes = Axes.Both,
            Direction = FillDirection.Vertical,
            Origin = Anchor.CentreLeft,
            Anchor = Anchor.CentreLeft,
            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(10, 0),
                    Children = new[]
                    {
                        CreateDrawableAccuracy(),
                        new FillFlowContainer
                        {
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
                                        Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold),
                                        Text = $"{ExtendedScore.LivePP:0}pp",
                                    },
                                },
                                new OsuSpriteText
                                {
                                    Font = OsuFont.GetFont(size: 10),
                                    Text = "live"
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}
