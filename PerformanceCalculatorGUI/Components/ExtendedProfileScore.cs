// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Leaderboards;
using osu.Game.Overlays;
using osu.Game.Overlays.Profile.Sections;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Utils;
using osuTK;

namespace PerformanceCalculatorGUI.Components
{
    public class ExtendedScore : APIScore
    {
        public double LivePP { get; }

        public Bindable<int> PositionChange { get; } = new();

        public PerformanceAttributes PerformanceAttributes { get; }

        public ExtendedScore(SoloScoreInfo score, double livePP, PerformanceAttributes attributes, WorkingBeatmap beatmap)
        {
            PerformanceAttributes = attributes;
            LivePP = livePP;

            TotalScore = score.TotalScore;
            MaxCombo = score.MaxCombo;
            User = score.User;
            OnlineID = score.OnlineID;
            HasReplay = score.HasReplay;
            Date = score.EndedAt ?? DateTimeOffset.MinValue;
            Beatmap = new APIBeatmap
            {
                OnlineID = beatmap.BeatmapInfo.OnlineID,
                DifficultyName = beatmap.BeatmapInfo.DifficultyName,
                BeatmapSet = new APIBeatmapSet
                {
                    Title = beatmap.Metadata.Title,
                    Artist = beatmap.Metadata.Artist,
                }
            };
            Accuracy = score.Accuracy;
            PP = score.PP;
            Statistics = new Dictionary<string, int>
            {
                { "count_300", score.Statistics.GetValueOrDefault(HitResult.Great) },
                { "count_100", score.Statistics.GetValueOrDefault(HitResult.Ok) },
                { "count_50", score.Statistics.GetValueOrDefault(HitResult.Meh) },
                { "count_miss", score.Statistics.GetValueOrDefault(HitResult.Miss) },
            };
            RulesetID = score.RulesetID;
            Mods = score.Mods;
            Rank = score.Rank;
        }
    }

    public class ExtendedProfileScore : CompositeDrawable
    {
        private const int height = 40;
        private const int performance_width = 100;
        private const int rank_difference_width = 35;
        private const int small_text_font_size = 11;

        private const float performance_background_shear = 0.45f;

        protected readonly ExtendedScore Score;

        [Resolved]
        private OsuColour colours { get; set; }

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; }

        private OsuSpriteText positionChangeText;

        public ExtendedProfileScore(ExtendedScore score)
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
                Children = new Drawable[]
                {
                    new Container
                    {
                        Name = "Rank difference",
                        RelativeSizeAxes = Axes.Y,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Width = rank_difference_width,
                        Child = positionChangeText = new OsuSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Colour = colourProvider.Light1,
                            Text = Score.PositionChange.Value.ToString()
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
                                    new UpdateableRank(Score.Rank)
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
                                            new ScoreBeatmapMetadataContainer(Score.Beatmap),
                                            new FillFlowContainer
                                            {
                                                AutoSizeAxes = Axes.Both,
                                                Direction = FillDirection.Horizontal,
                                                Spacing = new Vector2(15, 0),
                                                Children = new Drawable[]
                                                {
                                                    new OsuSpriteText
                                                    {
                                                        Text = $"{Score.Beatmap?.DifficultyName}",
                                                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.Regular),
                                                        Colour = colours.Yellow
                                                    },
                                                    new DrawableDate(Score.Date, 12)
                                                    {
                                                        Colour = colourProvider.Foreground1
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                AutoSizeAxes = Axes.X,
                                RelativeSizeAxes = Axes.Y,
                                Direction = FillDirection.Horizontal,
                                Children = new Drawable[]
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
                                                    Children = new Drawable[]
                                                    {
                                                        new FillFlowContainer
                                                        {
                                                            Anchor = Anchor.Centre,
                                                            Origin = Anchor.Centre,
                                                            Width = 110,
                                                            RelativeSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Vertical,
                                                            Children = new Drawable[]
                                                            {
                                                                new OsuSpriteText
                                                                {
                                                                    Text = Score.Accuracy.FormatAccuracy(),
                                                                    Font = OsuFont.GetFont(weight: FontWeight.Bold, italics: true),
                                                                    Colour = colours.Yellow,
                                                                    Anchor = Anchor.TopCentre,
                                                                    Origin = Anchor.TopCentre
                                                                },
                                                                new OsuSpriteText
                                                                {
                                                                    Text = $"{Score.MaxCombo}x {{ {Score.Statistics["count_300"]} / {Score.Statistics["count_100"]} / {Score.Statistics["count_50"]} / {Score.Statistics["count_miss"]} }}",
                                                                    Font = OsuFont.GetFont(size: small_text_font_size, weight: FontWeight.Regular),
                                                                    Colour = colourProvider.Light2,
                                                                    Anchor = Anchor.TopCentre,
                                                                    Origin = Anchor.TopCentre
                                                                },
                                                            }
                                                        },
                                                        new FillFlowContainer
                                                        {
                                                            Anchor = Anchor.Centre,
                                                            Origin = Anchor.Centre,
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
                                                                    Font = OsuFont.GetFont(size: small_text_font_size),
                                                                    Text = "live"
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
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
                                        Children = Score.Mods.Select(mod =>
                                        {
                                            var ruleset = rulesets.GetRuleset(Score.RulesetID) ?? throw new InvalidOperationException();

                                            return new ModIcon(ruleset.CreateInstance().CreateModFromAcronym(mod.Acronym))
                                            {
                                                Scale = new Vector2(0.35f)
                                            };
                                        }).ToList(),
                                    }
                                }
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
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                RelativeSizeAxes = Axes.Both,
                                Height = 0.5f,
                                Colour = colourProvider.Background4,
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
                                Colour = colourProvider.Background4,
                                Shear = new Vector2(performance_background_shear, 0),
                                EdgeSmoothness = new Vector2(2, 0),
                            },
                            new FillFlowContainer
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
                                        Text = $"{Score.PP:0}pp",
                                        Colour = colourProvider.Highlight1,
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre,
                                        TooltipContent = $"{AttributeConversion.ToReadableString(Score.PerformanceAttributes)}"
                                    },
                                    new OsuSpriteText
                                    {
                                        Font = OsuFont.GetFont(size: small_text_font_size),
                                        Text = $"{Score.PP - Score.LivePP:+0.0;-0.0;-}",
                                        Colour = colourProvider.Light1,
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre
                                    }
                                }
                            }
                        }
                    }
                }
            });

            Score.PositionChange.BindValueChanged(v => { positionChangeText.Text = $"{v.NewValue:+0;-0;-}"; });
        }

        private class ScoreBeatmapMetadataContainer : OsuHoverContainer
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
