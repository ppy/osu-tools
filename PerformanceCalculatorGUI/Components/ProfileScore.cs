// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Leaderboards;
using osu.Game.Overlays;
using osu.Game.Overlays.Profile.Sections;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using osu.Game.Utils;
using osuTK;
using PerformanceCalculatorGUI.Components.TextBoxes;

namespace PerformanceCalculatorGUI.Components
{
    public class ProfileScore
    {
        public SoloScoreInfo SoloScore { get; }

        public Bindable<int> Position { get; } = new Bindable<int>();

        public PerformanceAttributes PerformanceAttributes { get; }

        public ProfileScore(SoloScoreInfo score, PerformanceAttributes attributes)
        {
            SoloScore = score;
            PerformanceAttributes = attributes;
        }

        public ProfileScore(ScoreInfo score, PerformanceAttributes attributes)
        {
            SoloScore = toSoloScoreInfo(score);
            PerformanceAttributes = attributes;
        }

        private static SoloScoreInfo toSoloScoreInfo(ScoreInfo score)
        {
            APIBeatmapSet dummySet = new APIBeatmapSet
            {
                Title = score.BeatmapInfo?.Metadata.Title ?? "Error Title",
                TitleUnicode = score.BeatmapInfo?.Metadata.TitleUnicode ?? "Error Title",
                Artist = score.BeatmapInfo?.Metadata.Artist ?? "Error Artist",
                ArtistUnicode = score.BeatmapInfo?.Metadata.ArtistUnicode ?? "Error Artist",
            };
            APIBeatmap dummyBeatmap = new APIBeatmap
            {
                OnlineID = score.BeatmapInfo?.OnlineID ?? 0,
                DifficultyName = score.BeatmapInfo?.DifficultyName ?? "Error Difficulty",
            };
            SoloScoreInfo soloScoreInfo = new SoloScoreInfo
            {
                PP = score.PP,
                Accuracy = score.Accuracy,
                Rank = score.Rank,
                Statistics = score.Statistics,
                MaxCombo = score.MaxCombo,
                Mods = score.APIMods,
                Beatmap = dummyBeatmap,
                EndedAt = score.Date,
                BeatmapSet = dummySet,
            };

            return soloScoreInfo;
        }
    }

    public partial class DrawableProfileScore : CompositeDrawable
    {
        private const int height = 40;
        private const int rank_difference_width = 35;
        private const int performance_width = 100;
        private const int rank_width = 35;

        protected const int SMALL_TEXT_FONT_SIZE = 11;

        private const float performance_background_shear = 0.45f;

        protected FillFlowContainer RightInfoContainer;

        protected ProfileScore Score { get; }

        [Resolved]
        private OsuColour colours { get; set; }

        [Resolved]
        protected OverlayColourProvider ColourProvider { get; private set; }

        protected OsuSpriteText PositionText;

        public DrawableProfileScore(ProfileScore score)
        {
            Score = score;

            RelativeSizeAxes = Axes.X;
            Height = height;
        }

        [BackgroundDependencyLoader]
        private void load(RulesetStore rulesets)
        {
            AddInternal(new ProfileItemContainer
            {
                RelativeSizeAxes = Axes.Both,
                CornerRadius = ExtendedLabelledTextBox.CORNER_RADIUS,
                Children = new Drawable[]
                {
                    new Container
                    {
                        Name = "Rank difference",
                        RelativeSizeAxes = Axes.Y,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Width = rank_width,
                        Children = new Drawable[]
                        {
                            PositionText = new OsuSpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Colour = ColourProvider.Light1,
                                Text = Score.Position.Value.ToString()
                            }
                        }
                    },
                    new Container
                    {
                        Name = "Score info",
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Left = rank_difference_width, Right = performance_width },
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(10, 0),
                                Children = new Drawable[]
                                {
                                    new UpdateableRank(Score.SoloScore.Rank)
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(50, 20),
                                    },
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 0.5f),
                                        Children = new Drawable[]
                                        {
                                            new ScoreBeatmapMetadataContainer(Score.SoloScore.Beatmap),
                                            new FillFlowContainer
                                            {
                                                AutoSizeAxes = Axes.Both,
                                                Direction = FillDirection.Horizontal,
                                                Spacing = new Vector2(15, 0),
                                                Children = new Drawable[]
                                                {
                                                    new OsuSpriteText
                                                    {
                                                        Text = $"{Score.SoloScore.Beatmap?.DifficultyName}",
                                                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.Regular),
                                                        Colour = colours.Yellow
                                                    },
                                                    new DrawableDate(Score.SoloScore.EndedAt, 12)
                                                    {
                                                        Colour = ColourProvider.Foreground1
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            RightInfoContainer = new FillFlowContainer
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                AutoSizeAxes = Axes.X,
                                RelativeSizeAxes = Axes.Y,
                                Direction = FillDirection.Horizontal,
                                Padding = new MarginPadding { Right = 10 },
                                Children = CreateRightInfoContainerContent(rulesets)
                            }
                        }
                    },
                    new Container
                    {
                        Name = "Performance",
                        RelativeSizeAxes = Axes.Y,
                        Width = performance_width,
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        Children = new[]
                        {
                            new Box
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                RelativeSizeAxes = Axes.Both,
                                Height = 0.5f,
                                Colour = ColourProvider.Background4,
                                Shear = new Vector2(-performance_background_shear, 0),
                                EdgeSmoothness = new Vector2(2, 0),
                            },
                            new Box
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                RelativeSizeAxes = Axes.Both,
                                RelativePositionAxes = Axes.Y,
                                Height = -0.5f,
                                Position = new Vector2(0, 1),
                                Colour = ColourProvider.Background4,
                                Shear = new Vector2(performance_background_shear, 0),
                                EdgeSmoothness = new Vector2(2, 0),
                            },
                            CreatePerformanceInfo()
                        }
                    }
                }
            });
        }

        protected virtual Drawable[] CreateRightInfoContainerContent(RulesetStore rulesets)
        {
            return new Drawable[]
            {
                new Container
                {
                    AutoSizeAxes = Axes.X,
                    RelativeSizeAxes = Axes.Y,
                    Padding = new MarginPadding { Horizontal = 10, Vertical = 5 },
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Child = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Y,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 110,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = Score.SoloScore.Accuracy.FormatAccuracy(),
                                Font = OsuFont.GetFont(weight: FontWeight.Bold, italics: true),
                                Colour = colours.Yellow,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre
                            },
                            new OsuSpriteText
                            {
                                Text = $"{Score.SoloScore.MaxCombo}x {{ {formatStatistics(Score.SoloScore.Statistics)} }}",
                                Font = OsuFont.GetFont(size: SMALL_TEXT_FONT_SIZE, weight: FontWeight.Regular),
                                Colour = ColourProvider.Light2,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre
                            },
                        }
                    }
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(2),
                    Children = Score.SoloScore.Mods.Select(mod =>
                    {
                        var ruleset = rulesets.GetRuleset(Score.SoloScore.RulesetID) ?? throw new InvalidOperationException();
                        return new ModIcon(mod.ToMod(ruleset.CreateInstance()))
                        {
                            Scale = new Vector2(0.35f)
                        };
                    }).ToList(),
                }
            };
        }

        protected virtual Drawable CreatePerformanceInfo()
        {
            return new ExtendedOsuSpriteText
            {
                Padding = new MarginPadding
                {
                    Vertical = 5,
                    Left = 30,
                    Right = 20
                },
                Font = OsuFont.GetFont(weight: FontWeight.Bold),
                Text = $"{Score.SoloScore.PP:0}pp",
                Colour = ColourProvider.Highlight1,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                TooltipContent = $"{AttributeConversion.ToReadableString(Score.PerformanceAttributes)}"
            };
        }

        private static string formatStatistics(Dictionary<HitResult, int> statistics)
        {
            // TODO: ruleset-specific display
            return
                $"{statistics.GetValueOrDefault(HitResult.Great)} / {statistics.GetValueOrDefault(HitResult.Ok)} / {statistics.GetValueOrDefault(HitResult.Meh)} / {statistics.GetValueOrDefault(HitResult.Miss)}";
        }

        private partial class ScoreBeatmapMetadataContainer : OsuHoverContainer
        {
            private readonly IBeatmapInfo beatmapInfo;

            public ScoreBeatmapMetadataContainer(IBeatmapInfo beatmapInfo)
            {
                this.beatmapInfo = beatmapInfo;
                AutoSizeAxes = Axes.Both;
            }

            [BackgroundDependencyLoader(true)]
            private void load(GameHost host)
            {
                Action = () =>
                {
                    host.OpenUrlExternally($"https://osu.ppy.sh/b/{beatmapInfo.OnlineID}");
                };

                Child = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Text = new RomanisableString(beatmapInfo.Metadata.TitleUnicode, beatmapInfo.Metadata.Title),
                            Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold, italics: true)
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Text = " by ",
                            Font = OsuFont.GetFont(size: 12, italics: true)
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Text = new RomanisableString(beatmapInfo.Metadata.ArtistUnicode, beatmapInfo.Metadata.Artist),
                            Font = OsuFont.GetFont(size: 12, italics: true)
                        },
                    }
                };
            }
        }
    }
}
