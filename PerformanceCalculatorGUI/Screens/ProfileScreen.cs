// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Components.TextBoxes;
using PerformanceCalculatorGUI.Configuration;
using ButtonState = PerformanceCalculatorGUI.Components.ButtonState;

namespace PerformanceCalculatorGUI.Screens
{
    public partial class ProfileScreen : PerformanceCalculatorScreen
    {
        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Plum);

        private StatefulButton calculationButton;
        private SwitchButton includePinnedCheckbox;
        private SwitchButton onlyDisplayBestCheckbox;
        private VerboseLoadingLayer loadingLayer;

        private GridContainer layout;

        private FillFlowContainer<ExtendedProfileScore> scores;

        private LabelledTextBox usernameTextBox;
        private Container userPanelContainer;
        private UserCard userPanel;

        private string[] currentUsers = Array.Empty<string>();

        private CancellationTokenSource calculationCancellatonToken;

        private OverlaySortTabControl<ProfileSortCriteria> sortingTabControl;
        private readonly Bindable<ProfileSortCriteria> sorting = new Bindable<ProfileSortCriteria>(ProfileSortCriteria.Local);

        [Resolved]
        private NotificationDisplay notificationDisplay { get; set; }

        [Resolved]
        private APIManager apiManager { get; set; }

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        [Resolved]
        private SettingsManager configManager { get; set; }

        [Resolved]
        private RulesetStore rulesets { get; set; }

        public override bool ShouldShowConfirmationDialogOnSwitch => false;

        private const float username_container_height = 40;
        private const int max_api_scores = 200;
        private const int max_api_scores_in_one_query = 100;

        public ProfileScreen()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                layout = new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[] { new Dimension() },
                    RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Absolute, username_container_height),
                        new Dimension(GridSizeMode.Absolute),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension()
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new GridContainer
                            {
                                Name = "Settings",
                                Height = username_container_height,
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
                                        usernameTextBox = new ExtendedLabelledTextBox
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Anchor = Anchor.TopLeft,
                                            Label = "Username(s)",
                                            PlaceholderText = "peppy, rloseise, peppy2",
                                            CommitOnFocusLoss = false
                                        },
                                        calculationButton = new StatefulButton("Start calculation")
                                        {
                                            Width = 150,
                                            Height = username_container_height,
                                            Action = () => { calculateProfiles(usernameTextBox.Current.Value.Split(", ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)); }
                                        }
                                    }
                                }
                            },
                        },
                        new Drawable[]
                        {
                            userPanelContainer = new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y
                            }
                        },
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Children = new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Margin = new MarginPadding { Vertical = 2, Left = 10 },
                                        Spacing = new Vector2(5),
                                        Children = new Drawable[]
                                        {
                                            includePinnedCheckbox = new SwitchButton
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Current = { Value = true },
                                            },
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Font = OsuFont.Torus.With(weight: FontWeight.SemiBold, size: 14),
                                                UseFullGlyphHeight = false,
                                                Text = "Include pinned scores"
                                            },
                                            onlyDisplayBestCheckbox = new SwitchButton
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Current = { Value = true },
                                            },
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Font = OsuFont.Torus.With(weight: FontWeight.SemiBold, size: 14),
                                                UseFullGlyphHeight = false,
                                                Text = "Only display best score on each beatmap"
                                            }
                                        }
                                    },
                                    sortingTabControl = new OverlaySortTabControl<ProfileSortCriteria>
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Margin = new MarginPadding { Right = 22 },
                                        Current = { BindTarget = sorting },
                                        Alpha = 0
                                    }
                                }
                            }
                        },
                        new Drawable[]
                        {
                            new OsuScrollContainer(Direction.Vertical)
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = scores = new FillFlowContainer<ExtendedProfileScore>
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical
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

            usernameTextBox.OnCommit += (_, _) => { calculateProfiles(usernameTextBox.Current.Value.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)); };
            sorting.ValueChanged += e => { updateSorting(e.NewValue); };
            includePinnedCheckbox.Current.ValueChanged += e => { calculateProfiles(currentUsers); };
            onlyDisplayBestCheckbox.Current.ValueChanged += e => { calculateProfiles(currentUsers); };

            if (RuntimeInfo.IsDesktop)
                HotReloadCallbackReceiver.CompilationFinished += _ => Schedule(() => { calculateProfiles(currentUsers); });
        }

        private void calculateProfiles(string[] usernames)
        {
            currentUsers = usernames.Distinct().ToArray();

            if (usernames.Length < 1)
            {
                usernameTextBox.FlashColour(Color4.Red, 1);
                return;
            }

            calculationCancellatonToken?.Cancel();
            calculationCancellatonToken?.Dispose();

            loadingLayer.Show();
            calculationButton.State.Value = ButtonState.Loading;

            scores.Clear();

            calculationCancellatonToken = new CancellationTokenSource();
            var token = calculationCancellatonToken.Token;

            Task.Run(async () =>
            {
                Schedule(() =>
                {
                    if (userPanel != null)
                        userPanelContainer.Remove(userPanel, true);

                    sortingTabControl.Alpha = 1.0f;
                    sortingTabControl.Current.Value = ProfileSortCriteria.Local;

                    layout.RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Absolute, username_container_height),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension()
                    };
                });

                if (token.IsCancellationRequested)
                    return;

                var plays = new List<ExtendedScore>();
                var players = new List<APIUser>();
                var rulesetInstance = ruleset.Value.CreateInstance();

                foreach (string username in currentUsers)
                {
                    try
                    {
                        Schedule(() => loadingLayer.Text.Value = $"Getting {username} user data...");

                        var player = await apiManager.GetJsonFromApi<APIUser>($"users/{username}/{ruleset.Value.ShortName}").ConfigureAwait(false);
                        players.Add(player);

                        Schedule(() => loadingLayer.Text.Value = $"Calculating {player.Username} top scores...");

                        var apiScores = new List<SoloScoreInfo>();

                        for (int i = 0; i < max_api_scores; i += max_api_scores_in_one_query)
                        {
                            apiScores.AddRange(await apiManager.GetJsonFromApi<List<SoloScoreInfo>>($"users/{player.OnlineID}/scores/best?mode={ruleset.Value.ShortName}&limit={max_api_scores_in_one_query}&offset={i}").ConfigureAwait(false));
                            await Task.Delay(200, token).ConfigureAwait(false);
                        }

                        if (includePinnedCheckbox.Current.Value)
                        {
                            var pinnedScores = await apiManager.GetJsonFromApi<List<SoloScoreInfo>>($"users/{player.OnlineID}/scores/pinned?mode={ruleset.Value.ShortName}&limit={max_api_scores_in_one_query}")
                                                               .ConfigureAwait(false);
                            apiScores = apiScores.Concat(pinnedScores.Where(p => !apiScores.Any(b => b.ID == p.ID)).ToArray()).ToList();
                        }

                        foreach (var score in apiScores)
                        {
                            if (token.IsCancellationRequested)
                                return;

                            var working = ProcessorWorkingBeatmap.FromFileOrId(score.BeatmapID.ToString(), cachePath: configManager.GetBindable<string>(Settings.CachePath).Value);

                            Schedule(() => loadingLayer.Text.Value = $"Calculating {working.Metadata}");

                            Mod[] mods = score.Mods.Select(x => x.ToMod(rulesetInstance)).ToArray();

                            var scoreInfo = score.ToScoreInfo(rulesets, working.BeatmapInfo);

                            var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                            var difficultyCalculator = rulesetInstance.CreateDifficultyCalculator(working);
                            var difficultyAttributes = difficultyCalculator.Calculate(mods);
                            var performanceCalculator = rulesetInstance.CreatePerformanceCalculator();
                            if (performanceCalculator == null)
                                continue;

                            var perfAttributes = await performanceCalculator.CalculateAsync(parsedScore.ScoreInfo, difficultyAttributes, token).ConfigureAwait(false);
                            var extendedScore = new ExtendedScore(score, difficultyAttributes, perfAttributes);
                            plays.Add(extendedScore);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex.ToString(), level: LogLevel.Error);
                        notificationDisplay.Display(new Notification($"Failed to calculate {username}: {ex.Message}"));
                    }
                }

                if (token.IsCancellationRequested)
                    return;

                bool calculatingSingleProfile = players.Count == 1;

                // Add user card if only calculating single profile
                if (calculatingSingleProfile)
                {
                    Schedule(() =>
                    {
                        userPanelContainer.Add(userPanel = new UserCard(players[0])
                        {
                            RelativeSizeAxes = Axes.X
                        });
                    });
                }

                // Filter plays if only displaying best score on each beatmap
                if (onlyDisplayBestCheckbox.Current.Value)
                {
                    Schedule(() => loadingLayer.Text.Value = "Filtering plays");

                    var filteredPlays = new List<ExtendedScore>();

                    // List of all beatmap IDs in plays without duplicates
                    var beatmapIDs = plays.Select(x => x.SoloScore.BeatmapID).Distinct().ToList();

                    foreach (int id in beatmapIDs)
                    {
                        var bestPlayOnBeatmap = plays.Where(x => x.SoloScore.BeatmapID == id).OrderByDescending(x => x.PerformanceAttributes.Total).First();
                        filteredPlays.Add(bestPlayOnBeatmap);
                    }

                    plays = filteredPlays;
                }

                var localOrdered = plays.OrderByDescending(x => x.PerformanceAttributes.Total).ToList();
                var liveOrdered = plays.OrderByDescending(x => x.LivePP ?? 0).ToList();

                Schedule(() =>
                {
                    foreach (var play in plays)
                    {
                        scores.Add(new ExtendedProfileScore(play, !calculatingSingleProfile));

                        if (play.LivePP != null)
                        {
                            play.Position.Value = localOrdered.IndexOf(play) + 1;
                            play.PositionChange.Value = liveOrdered.IndexOf(play) - localOrdered.IndexOf(play);
                        }
                    }
                });

                if (calculatingSingleProfile)
                {
                    var player = players.First();

                    decimal totalLocalPP = 0;

                    for (int i = 0; i < localOrdered.Count; i++)
                        totalLocalPP += (decimal)(Math.Pow(0.95, i) * localOrdered[i].PerformanceAttributes.Total);

                    decimal totalLivePP = player.Statistics.PP ?? (decimal)0.0;

                    // https://github.com/ppy/osu-queue-score-statistics/blob/842653412d66eef527f7b7067b7cf50e886de954/osu.Server.Queues.ScoreStatisticsProcessor/Helpers/UserTotalPerformanceAggregateHelper.cs#L36-L38
                    // this might be slightly incorrect for some profiles due to the deduplication happening on the osu-queue-score-statistics side which we can't account for here
                    decimal playcountBonusPP = (decimal)((417.0 - 1.0 / 3.0) * (1.0 - Math.Pow(0.995, Math.Min(player.BeatmapPlayCountsCount, 1000))));
                    totalLocalPP += playcountBonusPP;

                    Schedule(() =>
                    {
                        userPanel.Data.Value = new UserCardData
                        {
                            LivePP = totalLivePP,
                            LocalPP = totalLocalPP,
                            PlaycountPP = playcountBonusPP
                        };
                    });
                }
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
                    updateSorting(ProfileSortCriteria.Local);
                });
            }, TaskContinuationOptions.None);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            calculationCancellatonToken?.Cancel();
            calculationCancellatonToken?.Dispose();
            calculationCancellatonToken = null;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape && !calculationCancellatonToken.IsCancellationRequested)
            {
                calculationCancellatonToken?.Cancel();
            }

            return base.OnKeyDown(e);
        }

        private void updateSorting(ProfileSortCriteria sortCriteria)
        {
            if (!scores.Children.Any())
                return;

            ExtendedProfileScore[] sortedScores;

            switch (sortCriteria)
            {
                case ProfileSortCriteria.Live:
                    sortedScores = scores.Children.OrderByDescending(x => x.Score.LivePP).ToArray();
                    break;

                case ProfileSortCriteria.Local:
                    sortedScores = scores.Children.OrderByDescending(x => x.Score.PerformanceAttributes.Total).ToArray();
                    break;

                case ProfileSortCriteria.Difference:
                    sortedScores = scores.Children.OrderByDescending(x => x.Score.PerformanceAttributes.Total - x.Score.LivePP).ToArray();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(sortCriteria), sortCriteria, null);
            }

            for (int i = 0; i < sortedScores.Length; i++)
            {
                scores.SetLayoutPosition(sortedScores[i], i);
            }
        }
    }
}
