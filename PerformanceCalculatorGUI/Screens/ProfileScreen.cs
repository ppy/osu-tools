// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using osu.Game.Users;
using PerformanceCalculatorGUI.Components;

namespace PerformanceCalculatorGUI.Screens
{
    internal class ProfileScreen : PerformanceCalculatorScreen
    {
        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Plum);

        private StatefulButton calculationButton;
        private VerboseLoadingLayer loadingLayer;

        private FillFlowContainer scores;

        private LabelledTextBox usernameTextBox;

        private FillFlowContainer layout;

        private UserListPanel userPanel;

        private readonly Bindable<APIUser> user = new Bindable<APIUser>();

        [Resolved]
        private APIManager apiManager { get; set; }

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        public override bool ShouldShowConfirmationDialogOnSwitch => false;

        public ProfileScreen()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                layout = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        new GridContainer
                        {
                            Name = "Settings",
                            Height = 40,
                            RelativeSizeAxes = Axes.X,
                            ColumnDimensions = new[]
                            {
                                new Dimension(),
                                new Dimension(GridSizeMode.AutoSize)
                            },
                            RowDimensions = new[]
                            {
                                new Dimension(GridSizeMode.AutoSize)
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    usernameTextBox = new LabelledTextBox()
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        Label = "Username",
                                        PlaceholderText = "peppy"
                                    },
                                    calculationButton = new StatefulButton("Start calculation")
                                    {
                                        Width = 150,
                                        Height = 40,
                                        Action = calculateProfile
                                    }
                                }
                            }
                        },
                    }
                },
                loadingLayer = new VerboseLoadingLayer(true)
                {
                    RelativeSizeAxes = Axes.Both
                }
            };

            // score container is being added separately so that flow container can properly assign order IDs
            layout.Insert(2, new OsuScrollContainer(Direction.Vertical)
            {
                RelativeSizeAxes = Axes.Both,
                Child = scores = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical
                }
            });
        }

        private void calculateProfile()
        {
            loadingLayer.Show();
            calculationButton.State.Value = ButtonState.Loading;

            scores.Clear();

            Task.Run(async () =>
            {
                Schedule(() => loadingLayer.Text.Value = "Getting user data...");

                var player = await apiManager.GetJsonFromApi<APIUser>($"users/{usernameTextBox.Current.Value}/{ruleset.Value.ShortName}");

                user.Value = player;

                if (userPanel != null)
                    layout.Remove(userPanel);

                Schedule(() => layout.Insert(1, userPanel = new UserListPanel(user.Value)
                {
                    RelativeSizeAxes = Axes.X
                }));

                var plays = new List<ExtendedScore>();

                var rulesetInstance = ruleset.Value.CreateInstance();

                Schedule(() => loadingLayer.Text.Value = $"Calculating {player.Username} top scores...");

                var apiScores = await apiManager.GetJsonFromApi<List<APIScore>>($"users/{player.OnlineID}/scores/best?mode={ruleset.Value.ShortName}&limit=100");

                foreach (var score in apiScores)
                {
                    await Task.Run(() =>
                    {
                        var working = ProcessorWorkingBeatmap.FromFileOrId(score.Beatmap?.OnlineID.ToString());

                        Schedule(() => loadingLayer.Text.Value = $"Calculating {working.Metadata}");

                        var modsAcronyms = score.Mods.Select(x => x.ToString()).ToArray();
                        Mod[] mods = rulesetInstance.CreateAllMods().Where(m => modsAcronyms.Contains(m.Acronym)).ToArray();

                        var scoreInfo = new ScoreInfo(working.BeatmapInfo, ruleset.Value)
                        {
                            TotalScore = score.TotalScore,
                            MaxCombo = score.MaxCombo,
                            Mods = mods,
                            Statistics = new Dictionary<HitResult, int>()
                        };

                        scoreInfo.SetCount300(score.Statistics["count_300"]);
                        scoreInfo.SetCountGeki(score.Statistics["count_geki"]);
                        scoreInfo.SetCount100(score.Statistics["count_100"]);
                        scoreInfo.SetCountKatu(score.Statistics["count_katu"]);
                        scoreInfo.SetCount50(score.Statistics["count_50"]);
                        scoreInfo.SetCountMiss(score.Statistics["count_miss"]);

                        var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                        var difficultyCalculator = rulesetInstance.CreateDifficultyCalculator(working);
                        var difficultyAttributes = difficultyCalculator.Calculate(scoreInfo.Mods);
                        var performanceCalculator = rulesetInstance.CreatePerformanceCalculator(difficultyAttributes, parsedScore.ScoreInfo);

                        var livePp = score.PP ?? 0.0;
                        score.PP = performanceCalculator?.Calculate().Total ?? 0.0;

                        var extendedScore = new ExtendedScore(score, livePp);
                        plays.Add(extendedScore);

                        Schedule(() => scores.Add(new ExtendedProfileScore(extendedScore)));
                    });
                }

                var localOrdered = plays.Select(x => x.PP).OrderByDescending(x => x).ToList();
                var liveOrdered = plays.Select(x => x.PP).OrderByDescending(x => x).ToList();

                int index = 0;
                decimal totalLocalPP = (decimal)localOrdered.Sum(play => Math.Pow(0.95, index++) * play);
                decimal totalLivePP = player.Statistics.PP ?? (decimal)0.0;

                index = 0;
                decimal nonBonusLivePP = (decimal)liveOrdered.Sum(play => Math.Pow(0.95, index++) * play);

                //todo: implement properly. this is pretty damn wrong.
                var playcountBonusPP = (totalLivePP - nonBonusLivePP);
                totalLocalPP += playcountBonusPP;

                user.Value.Statistics.PP = totalLocalPP;
            }).ContinueWith(t =>
            {
                Schedule(() =>
                {
                    loadingLayer.Hide();
                    calculationButton.State.Value = ButtonState.Done;
                });
            });
        }
    }
}
