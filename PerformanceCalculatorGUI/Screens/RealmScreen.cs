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
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Components.TextBoxes;
using PerformanceCalculatorGUI.Configuration;
using PerformanceCalculatorGUI.Screens.Profile;
using ButtonState = PerformanceCalculatorGUI.Components.ButtonState;

namespace PerformanceCalculatorGUI.Screens
{
    public partial class RealmScreen : PerformanceCalculatorScreen
    {
        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Plum);

        private StatefulButton calculationButton = null!;
        private SwitchButton includeUnrankedMaps = null!;
        private SwitchButton includeUnrankedMods = null!;
        private SwitchButton onlyDisplayBestCheckbox = null!;
        private VerboseLoadingLayer loadingLayer = null!;

        private GridContainer layout = null!;

        private FillFlowContainer<ExtendedProfileScore> scores = null!;

        private LabelledTextBox usernameTextBox = null!;
        private Container userPanelContainer = null!;
        private UserCard? userPanel;

        private string username = "";

        private CancellationTokenSource? calculationCancellatonToken;

        [Resolved]
        private NotificationDisplay notificationDisplay { get; set; } = null!;

        [Resolved]
        private APIManager apiManager { get; set; } = null!;

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private SettingsManager configManager { get; set; } = null!;

        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        [Resolved]
        private GameHost gameHost { get; set; } = null!;

        public override bool ShouldShowConfirmationDialogOnSwitch => false;

        private const float username_container_height = 40;

        public RealmScreen()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background6
                },
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
                                            Label = "Username",
                                            PlaceholderText = "peppy",
                                            CommitOnFocusLoss = false
                                        },
                                        calculationButton = new StatefulButton("Start calculation")
                                        {
                                            Width = 150,
                                            Height = username_container_height,
                                            Action = () => { calculateProfile(usernameTextBox.Current.Value); }
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
                                            includeUnrankedMaps = new SwitchButton
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
                                                Text = "Include unranked maps"
                                            },
                                            includeUnrankedMods = new SwitchButton
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
                                                Text = "Include unranked mods"
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

            usernameTextBox.OnCommit += (_, _) => { calculateProfile(usernameTextBox.Current.Value); };
            includeUnrankedMaps.Current.ValueChanged += e => { calculateProfile(username); };
            includeUnrankedMods.Current.ValueChanged += e => { calculateProfile(username); };
            onlyDisplayBestCheckbox.Current.ValueChanged += e => { calculateProfile(username); };

            if (RuntimeInfo.IsDesktop)
                HotReloadCallbackReceiver.CompilationFinished += _ => Schedule(() => { calculateProfile(username); });
        }

        private void calculateProfile(string username)
        {
            if (username == "")
            {
                usernameTextBox.FlashColour(Color4.Red, 1);
                return;
            }

            var storage = gameHost.GetStorage(configManager.GetBindable<string>(Settings.RealmPath).Value);
            var realmAccess = new RealmAccess(storage, @"client.realm");

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
                APIUser player = null;
                var rulesetInstance = ruleset.Value.CreateInstance();
                string[] playerUsernames;
                
                try
                {
                    Schedule(() => loadingLayer.Text.Value = $"Getting {username} user data...");

                    player = await apiManager.GetJsonFromApi<APIUser>($"users/{username}/{ruleset.Value.ShortName}").ConfigureAwait(false);
                    playerUsernames = [player.Username, .. player.PreviousUsernames, player.Id.ToString()];

                    Schedule(() => loadingLayer.Text.Value = $"Calculating {player.Username} top scores...");

                    var realmScores = realmAccess.Run(r => r.All<ScoreInfo>().Detach())
                                                 .Where(x => playerUsernames.Any(name => name.Equals(x.User.Username, StringComparison.OrdinalIgnoreCase)) && // scores from the correct user
                                                             x.BeatmapInfo != null && // map exists
                                                             x.Passed == true && x.Rank != ScoreRank.F && // exclude failed scores
                                                             x.Ruleset.OnlineID == ruleset.Value.OnlineID && // exclude other rulesets
                                                             x.BeatmapInfo.OnlineID != -1) // exclude unsubmitted maps
                                                 .ToList();
                    // remove unranked maps and mods if toggled off
                    if (!includeUnrankedMaps.Current.Value)
                        realmScores.RemoveAll(x => x.BeatmapInfo.Status != BeatmapOnlineStatus.Ranked);
                    if (!includeUnrankedMods.Current.Value)
                        realmScores.RemoveAll(x => x.Mods.Any(mod => !mod.Ranked));

                    foreach (var score in realmScores)
                    {
                        if (token.IsCancellationRequested)
                            return;

                        var working = ProcessorWorkingBeatmap.FromFileOrId(score.BeatmapInfo.OnlineID.ToString(), cachePath: configManager.GetBindable<string>(Settings.CachePath).Value);

                        Schedule(() => loadingLayer.Text.Value = $"Calculating {working.Metadata}");

                        var difficultyCalculator = rulesetInstance.CreateDifficultyCalculator(working);
                        var difficultyAttributes = difficultyCalculator.Calculate(score.Mods);
                        var performanceCalculator = rulesetInstance.CreatePerformanceCalculator();
                        if (performanceCalculator == null)
                            continue;

                        var perfAttributes = await performanceCalculator.CalculateAsync(score, difficultyAttributes, token).ConfigureAwait(false);
                        var extendedScore = new ExtendedScore(score, difficultyAttributes, perfAttributes);
                        plays.Add(extendedScore);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString(), level: LogLevel.Error);
                    notificationDisplay.Display(new Notification($"Failed to calculate {username}: {ex.Message}"));
                }
            

                if (token.IsCancellationRequested)
                    return;

                Schedule(() =>
                {
                    userPanelContainer.Add(userPanel = new UserCard(player)
                    {
                        RelativeSizeAxes = Axes.X
                    });
                });

                // Filter plays if only displaying best score on each beatmap
                if (onlyDisplayBestCheckbox.Current.Value)
                {
                    Schedule(() => loadingLayer.Text.Value = "Filtering plays");

                    var filteredPlays = new List<ExtendedScore>();

                    // List of all beatmap IDs in plays without duplicates
                    var beatmapIDs = plays.Select(x => x.Score.Beatmap.OnlineID).Distinct().ToList();

                    foreach (int id in beatmapIDs)
                    {
                        var bestPlayOnBeatmap = plays.Where(x => x.Score.Beatmap.OnlineID == id).OrderByDescending(x => x.PerformanceAttributes?.Total).First();
                        filteredPlays.Add(bestPlayOnBeatmap);
                    }

                    plays = filteredPlays;
                }

                plays = plays.OrderByDescending(x => x.PerformanceAttributes?.Total).ToList();

                Schedule(() =>
                {
                    foreach (var play in plays)
                    {
                        scores.Add(new ExtendedProfileScore(play, false));

                        play.Position.Value = plays.IndexOf(play) + 1;
                    }
                });

                decimal totalLocalPP = 0;

                for (int i = 0; i < plays.Count; i++)
                    totalLocalPP += (decimal)(Math.Pow(0.95, i) * plays[i].PerformanceAttributes?.Total ?? 0);

                decimal totalLivePP = player.Statistics.PP ?? (decimal)0.0;

                // https://github.com/ppy/osu-queue-score-statistics/blob/842653412d66eef527f7b7067b7cf50e886de954/osu.Server.Queues.ScoreStatisticsProcessor/Helpers/UserTotalPerformanceAggregateHelper.cs#L36-L38
                // this might be slightly incorrect for some profiles due to the deduplication happening on the osu-queue-score-statistics side which we can't account for here
                decimal playcountBonusPP = (decimal)((417.0 - 1.0 / 3.0) * (1.0 - Math.Pow(0.995, Math.Min(player.BeatmapPlayCountsCount, 1000))));
                totalLocalPP += playcountBonusPP;

                Schedule(() =>
                {
                    if (userPanel != null)
                    {
                        userPanel.Data.Value = new UserCardData
                        {
                            LivePP = totalLivePP,
                            LocalPP = totalLocalPP,
                            PlaycountPP = playcountBonusPP
                        };
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
            if (e.Key == Key.Escape && calculationCancellatonToken?.IsCancellationRequested == false)
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
                    sortedScores = scores.Children.OrderByDescending(x => x.ExtScore.LivePP).ToArray();
                    break;

                case ProfileSortCriteria.Local:
                    sortedScores = scores.Children.OrderByDescending(x => x.ExtScore.PerformanceAttributes?.Total).ToArray();
                    break;

                case ProfileSortCriteria.Difference:
                    sortedScores = scores.Children.OrderByDescending(x => x.ExtScore.PerformanceAttributes?.Total - x.ExtScore.LivePP).ToArray();
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
