// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Users;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Components.TextBoxes;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Screens
{
    public class UserLeaderboardData
    {
        public decimal LivePP { get; set; }
        public decimal LocalPP { get; set; }

        public List<ExtendedScore> Scores { get; set; }
    }

    public partial class LeaderboardScreen : PerformanceCalculatorScreen
    {
        public enum Tabs
        {
            Players,
            Scores
        }

        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Green);

        private LimitedLabelledNumberBox playerAmountTextBox;
        private LimitedLabelledNumberBox pageTextBox;
        private StatefulButton calculationButton;
        private VerboseLoadingLayer loadingLayer;

        private Container players;
        private FillFlowContainer scores;
        private OsuTabControl<Tabs> tabs;

        private CancellationTokenSource calculationCancellatonToken;

        public override bool ShouldShowConfirmationDialogOnSwitch => players.Count > 0;

        [Resolved]
        private NotificationDisplay notificationDisplay { get; set; }

        [Resolved]
        private APIManager apiManager { get; set; }

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        [Resolved]
        private RulesetStore rulesets { get; set; }

        [Resolved]
        private SettingsManager configManager { get; set; }

        private const int settings_height = 40;
        private const int tabs_height = 20;

        public LeaderboardScreen()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[] { new Dimension() },
                    RowDimensions = new[] { new Dimension(GridSizeMode.Absolute, 40), new Dimension() },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new GridContainer
                            {
                                Name = "Settings",
                                Height = settings_height,
                                RelativeSizeAxes = Axes.X,
                                ColumnDimensions = new[]
                                {
                                    new Dimension(),
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
                                        playerAmountTextBox = new LimitedLabelledNumberBox
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Anchor = Anchor.TopLeft,
                                            Label = "Amount",
                                            PlaceholderText = "10",
                                            MinValue = 1,
                                            MaxValue = 50,
                                            Value = { Value = 10 },
                                            CommitOnFocusLoss = false
                                        },
                                        pageTextBox = new LimitedLabelledNumberBox
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Anchor = Anchor.TopLeft,
                                            Label = "Page",
                                            PlaceholderText = "1",
                                            MinValue = 1,
                                            Value = { Value = 1 },
                                            CommitOnFocusLoss = false
                                        },
                                        calculationButton = new StatefulButton("Start calculation")
                                        {
                                            Width = 150,
                                            Height = settings_height,
                                            Action = calculate
                                        }
                                    }
                                }
                            }
                        },
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    tabs = new OsuTabControl<Tabs>
                                    {
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre,
                                        Height = tabs_height,
                                        Width = 145,
                                        IsSwitchable = true
                                    },
                                    new OsuScrollContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Children = new Drawable[]
                                        {
                                            players = new Container
                                            {
                                                RelativeSizeAxes = Axes.Both
                                            },
                                            scores = new FillFlowContainer
                                            {
                                                Margin = new MarginPadding { Top = tabs_height },
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Direction = FillDirection.Vertical,
                                            }
                                        }
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

            scores.Hide();

            tabs.Current.ValueChanged += e =>
            {
                switch (e.NewValue)
                {
                    case Tabs.Players:
                    {
                        scores.Hide();
                        players.Show();
                        break;
                    }

                    case Tabs.Scores:
                    {
                        scores.Show();
                        players.Hide();
                        break;
                    }
                }
            };
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            calculationCancellatonToken?.Cancel();
            calculationCancellatonToken?.Dispose();
            calculationCancellatonToken = null;
        }

        private void calculate()
        {
            calculationCancellatonToken?.Cancel();
            calculationCancellatonToken?.Dispose();

            loadingLayer.Show();
            calculationButton.State.Value = ButtonState.Loading;

            players.Clear();
            scores.Clear();

            calculationCancellatonToken = new CancellationTokenSource();
            var token = calculationCancellatonToken.Token;

            Task.Run(async () =>
            {
                Schedule(() => loadingLayer.Text.Value = "Getting leaderboard...");

                var leaderboard = await apiManager.GetJsonFromApi<GetTopUsersResponse>($"rankings/{ruleset.Value.ShortName}/performance?cursor[page]={pageTextBox.Value.Value - 1}");

                var calculatedPlayers = new List<LeaderboardUser>();
                var calculatedScores = new List<ExtendedScore>();

                for (int i = 0; i < playerAmountTextBox.Value.Value; i++)
                {
                    if (token.IsCancellationRequested)
                        return;

                    var player = leaderboard.Users[i];

                    Schedule(() => loadingLayer.Text.Value = $"Calculating {player.User.Username} top scores...");

                    var playerData = await calculatePlayer(player, token);

                    calculatedPlayers.Add(new LeaderboardUser
                    {
                        User = player.User,
                        LocalPP = playerData.LocalPP,
                        LivePP = playerData.LivePP,
                        Difference = playerData.LocalPP - playerData.LivePP
                    });

                    calculatedScores.AddRange(playerData.Scores);
                }

                Schedule(() =>
                {
                    var leaderboardTable = new LeaderboardTable(pageTextBox.Value.Value, calculatedPlayers.OrderByDescending(x => x.LocalPP).ToList())
                    {
                        Margin = new MarginPadding { Top = tabs_height }
                    };
                    LoadComponent(leaderboardTable);
                    players.Add(leaderboardTable);

                    foreach (var calculatedScore in calculatedScores.OrderByDescending(x => x.PerformanceAttributes.Total))
                    {
                        scores.Add(new ExtendedProfileScore(calculatedScore));
                    }
                });
            }, token).ContinueWith(t =>
            {
                Logger.Log(t.Exception?.ToString(), level: LogLevel.Error);
                notificationDisplay.Display(new Notification(t.Exception?.Flatten().Message));
            }, TaskContinuationOptions.OnlyOnFaulted).ContinueWith(t =>
            {
                Schedule(() =>
                {
                    loadingLayer.Hide();
                    calculationButton.State.Value = ButtonState.Done;
                });
            }, token);
        }

        private async Task<UserLeaderboardData> calculatePlayer(UserStatistics player, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return new UserLeaderboardData();

            var plays = new List<ExtendedScore>();

            var apiScores = await apiManager.GetJsonFromApi<List<SoloScoreInfo>>($"users/{player.User.OnlineID}/scores/best?mode={ruleset.Value.ShortName}&limit=100");

            var rulesetInstance = ruleset.Value.CreateInstance();

            try
            {
                Parallel.ForEach(apiScores, new ParallelOptions { CancellationToken = token }, score =>
                {
                    try
                    {
                        var working = ProcessorWorkingBeatmap.FromFileOrId(score.BeatmapID.ToString(), cachePath: configManager.GetBindable<string>(Settings.CachePath).Value);

                        Mod[] mods = score.Mods.Select(x => x.ToMod(rulesetInstance)).ToArray();

                        var scoreInfo = score.ToScoreInfo(rulesets, working.BeatmapInfo);

                        var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                        var difficultyCalculator = rulesetInstance.CreateDifficultyCalculator(working);
                        var difficultyAttributes = difficultyCalculator.Calculate(RulesetHelper.ConvertToLegacyDifficultyAdjustmentMods(rulesetInstance, mods));
                        var performanceCalculator = rulesetInstance.CreatePerformanceCalculator();

                        var livePp = score.PP ?? 0.0;
                        var perfAttributes = performanceCalculator?.Calculate(parsedScore.ScoreInfo, difficultyAttributes);
                        score.PP = perfAttributes?.Total ?? 0.0;

                        var extendedScore = new ExtendedScore(score, livePp, perfAttributes);
                        plays.Add(extendedScore);
                    }
                    catch (Exception e)
                    {
                        if (e is WebException)
                        {
                            // web exception usually means we hit rate limiting in which case we wanna bail immediately
                            throw;
                        }

                        Logger.Log(e.ToString(), level: LogLevel.Error);
                        notificationDisplay.Display(new Notification(e.Message));
                    }
                });
            }
            catch (OperationCanceledException) { }

            var localOrdered = plays.OrderByDescending(x => x.SoloScore.PP).ToList();
            var liveOrdered = plays.OrderByDescending(x => x.LivePP).ToList();

            int index = 0;
            decimal totalLocalPP = (decimal)(localOrdered.Select(x => x.SoloScore.PP).Sum(play => Math.Pow(0.95, index++) * play) ?? 0.0);
            decimal totalLivePP = player.PP ?? (decimal)0.0;

            index = 0;
            decimal nonBonusLivePP = (decimal)liveOrdered.Select(x => x.LivePP).Sum(play => Math.Pow(0.95, index++) * play);

            //todo: implement properly. this is pretty damn wrong.
            var playcountBonusPP = (totalLivePP - nonBonusLivePP);
            totalLocalPP += playcountBonusPP;

            return new UserLeaderboardData
            {
                LivePP = totalLivePP,
                LocalPP = totalLocalPP,
                Scores = plays
            };
        }
    }
}
