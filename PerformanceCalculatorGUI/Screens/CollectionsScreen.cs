// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osuTK;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Configuration;
using PerformanceCalculatorGUI.Screens.Collections;

namespace PerformanceCalculatorGUI.Screens
{
    public partial class CollectionsScreen : PerformanceCalculatorScreen
    {
        public override bool ShouldShowConfirmationDialogOnSwitch => false;

        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

        [Resolved]
        private ScoreCache scoreCache { get; set; } = null!;

        [Resolved]
        private SettingsManager configManager { get; set; } = null!;

        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        [Resolved]
        private DialogOverlay dialogOverlay { get; set; } = null!;

        [Resolved]
        private NotificationDisplay notificationDisplay { get; set; } = null!;

        private FillFlowContainer collectionList = null!;
        private CreateCollectionButton createCollectionButton = null!;

        private OsuSpriteText collectionNameText = null!;
        private FillFlowContainer collectionContainer = null!;
        private FillFlowContainer<ScoreContainer> scoresList = null!;
        private AddScoreButton addScoreButton = null!;
        private readonly Bindable<CollectionSortCriteria> sorting = new Bindable<CollectionSortCriteria>(CollectionSortCriteria.None);

        private VerboseLoadingLayer loadingLayer = null!;

        private readonly Bindable<Collection?> currentCollection = new Bindable<Collection?>();

        private const string collections_directory = "collections";

        public CollectionsScreen()
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
                    ColumnDimensions = new[] { new Dimension(GridSizeMode.Absolute, 250), new Dimension() },
                    RowDimensions = new[] { new Dimension() },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colourProvider.Background6.Darken(0.2f)
                                    },
                                    new OsuScrollContainer(Direction.Vertical)
                                    {
                                        Name = "Collection List",
                                        RelativeSizeAxes = Axes.Both,
                                        Children = new Drawable[]
                                        {
                                            new FillFlowContainer
                                            {
                                                Padding = new MarginPadding { Left = 10f, Right = 15.0f, Vertical = 5f },
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Direction = FillDirection.Vertical,
                                                Spacing = new Vector2(0, 2f),
                                                Children = new Drawable[]
                                                {
                                                    new OsuSpriteText
                                                    {
                                                        Origin = Anchor.TopCentre,
                                                        Anchor = Anchor.TopCentre,
                                                        Height = 20,
                                                        Text = "Collection list"
                                                    },
                                                    collectionList = new FillFlowContainer
                                                    {
                                                        RelativeSizeAxes = Axes.X,
                                                        AutoSizeAxes = Axes.Y,
                                                        Direction = FillDirection.Vertical,
                                                    },
                                                    createCollectionButton = new CreateCollectionButton()
                                                }
                                            }
                                        }
                                    },
                                }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colourProvider.Background6
                                    },
                                    new OsuScrollContainer(Direction.Vertical)
                                    {
                                        Name = "Scores",
                                        RelativeSizeAxes = Axes.Both,
                                        Child = collectionContainer = new FillFlowContainer
                                        {
                                            Padding = new MarginPadding { Left = 10f, Right = 15.0f, Vertical = 5f },
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 2f),
                                            Alpha = 0,
                                            Children =
                                            [
                                                new Container
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Children = new Drawable[]
                                                    {
                                                        collectionNameText = new OsuSpriteText
                                                        {
                                                            Origin = Anchor.TopLeft,
                                                            Anchor = Anchor.TopLeft,
                                                            Height = 20
                                                        },
                                                        new OverlaySortTabControl<CollectionSortCriteria>
                                                        {
                                                            Anchor = Anchor.CentreRight,
                                                            Origin = Anchor.CentreRight,
                                                            Margin = new MarginPadding { Right = 20 },
                                                            Current = { BindTarget = sorting }
                                                        }
                                                    }
                                                },
                                                scoresList = new FillFlowContainer<ScoreContainer>
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                },
                                                addScoreButton = new AddScoreButton()
                                            ]
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                loadingLayer = new VerboseLoadingLayer(true)
                {
                    RelativeSizeAxes = Axes.Both
                }
            };
            sorting.ValueChanged += e => { updateSorting(e.NewValue); };

            currentCollection.ValueChanged += loadCollection;
            createCollectionButton.OnSave += onCollectionAdd;
            addScoreButton.OnAdd += onScoreAdd;

            loadCollectionList();

            if (RuntimeInfo.IsDesktop)
                HotReloadCallbackReceiver.CompilationFinished += _ => Schedule(calculateScores);
        }

        private void onScoreAdd(long scoreId)
        {
            if (currentCollection.Value!.Scores.Contains(scoreId))
            {
                notificationDisplay.Display(new Notification($"Score {scoreId} already exists"));
                return;
            }

            currentCollection.Value.Scores = [..currentCollection.Value.Scores, scoreId];

            saveCurrentCollection();
        }

        private void onScoreRemove(long scoreId)
        {
            currentCollection.Value!.Scores = currentCollection.Value.Scores.Where(x => x != scoreId).ToArray();

            saveCurrentCollection();
        }

        private void loadCollection(ValueChangedEvent<Collection?> obj)
        {
            if (obj.NewValue == null)
            {
                collectionContainer.Hide();
                return;
            }

            collectionNameText.Text = obj.NewValue!.Name;
            collectionContainer.Show();

            calculateScores();
        }

        private void saveCurrentCollection()
        {
            if (currentCollection.Value == null)
                return;

            string path = Path.Combine(collections_directory, currentCollection.Value.FileName);

            File.WriteAllText(path, JsonConvert.SerializeObject(currentCollection.Value));

            calculateScores();
        }

        private void calculateScores()
        {
            if (currentCollection.Value == null)
                return;

            scoresList.Clear();

            loadingLayer.Show();

            Task.Run(async () =>
            {
                foreach (long scoreId in currentCollection.Value.Scores)
                {
                    var score = await scoreCache.GetScore(scoreId).ConfigureAwait(false);
                    if (score == null)
                        continue;

                    var rulesetInstance = rulesets.GetRuleset(score.RulesetID)!.CreateInstance();

                    var working = ProcessorWorkingBeatmap.FromFileOrId(score.BeatmapID.ToString(), cachePath: configManager.GetBindable<string>(Settings.CachePath).Value);

                    Mod[] mods = score.Mods.Select(x => x.ToMod(rulesetInstance)).ToArray();

                    var scoreInfo = score.ToScoreInfo(rulesets, working.BeatmapInfo);

                    var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                    var difficultyCalculator = rulesetInstance.CreateDifficultyCalculator(working);
                    var difficultyAttributes = difficultyCalculator.Calculate(mods);
                    var performanceCalculator = rulesetInstance.CreatePerformanceCalculator();
                    if (performanceCalculator == null)
                        continue;

                    var perfAttributes = performanceCalculator.Calculate(parsedScore.ScoreInfo, difficultyAttributes);
                    Schedule(() =>
                    {
                        var scoreContainer = new ScoreContainer(new ExtendedScore(score, difficultyAttributes, perfAttributes));
                        scoreContainer.OnDelete += onScoreRemove;

                        scoresList.Add(scoreContainer);
                    });
                }
            }).ContinueWith(t =>
            {
                Logger.Log(t.Exception?.ToString(), level: LogLevel.Error);
                notificationDisplay.Display(new Notification(t.Exception?.Flatten().Message ?? "Failed to calculate collection"));
            }, TaskContinuationOptions.OnlyOnFaulted).ContinueWith(t =>
            {
                Schedule(() =>
                {
                    updateSorting(sorting.Value);
                    loadingLayer.Hide();
                });
            }, TaskContinuationOptions.None);
        }

        private void onCollectionAdd(string name)
        {
            string fileName = RandomNumberGenerator.GetString(choices: "abcdefghijklmnopqrstuvwxyz0123456789", length: 16) + ".json";

            var collection = new Collection
            {
                Name = name,
                FileName = fileName,
                Scores = []
            };

            string path = Path.Combine(collections_directory, fileName);

            File.WriteAllText(path, JsonConvert.SerializeObject(collection));

            loadCollectionList();
        }

        private void loadCollectionList()
        {
            if (!Directory.Exists(collections_directory))
            {
                Directory.CreateDirectory(collections_directory);

                return; // nothing to load
            }

            collectionList.Clear();

            var collections = new List<Collection>();

            foreach (string collectionFile in Directory.EnumerateFiles(collections_directory))
            {
                var deserializedCollection = JsonConvert.DeserializeObject<Collection>(File.ReadAllText(collectionFile));

                if (deserializedCollection != null)
                {
                    collections.Add(deserializedCollection);
                }
            }

            foreach (var collection in collections.OrderBy(x => x.Name))
            {
                var collectionButton = new CollectionButton(collection, currentCollection);
                collectionList.Add(collectionButton);

                collectionButton.OnDelete += onCollectionDelete;
            }
        }

        private void onCollectionDelete(Collection collection)
        {
            dialogOverlay.Push(new ConfirmDialog("", () =>
            {
                if (collection == currentCollection.Value)
                    currentCollection.Value = null;

                File.Delete(Path.Combine(collections_directory, collection.FileName));

                loadCollectionList();
            })
            {
                HeaderText = DialogStrings.DeletionHeaderText,
                Icon = FontAwesome.Solid.Trash,
                BodyText = collection.Name
            });
        }

        private void updateSorting(CollectionSortCriteria sortCriteria)
        {
            if (!scoresList.Children.Any())
                return;

            if (sortCriteria == CollectionSortCriteria.None)
            {
                for (int i = 0; i < scoresList.Count; i++)
                {
                    scoresList.SetLayoutPosition(scoresList[i], Array.IndexOf(currentCollection.Value!.Scores, scoresList[i].Score.SoloScore.ID));
                }

                return;
            }

            ScoreContainer[] sortedScores;

            switch (sortCriteria)
            {
                case CollectionSortCriteria.Live:
                    sortedScores = scoresList.Children.OrderByDescending(x => x.Score.LivePP).ToArray();
                    break;

                case CollectionSortCriteria.Local:
                    sortedScores = scoresList.Children.OrderByDescending(x => x.Score.PerformanceAttributes?.Total).ToArray();
                    break;

                case CollectionSortCriteria.Difference:
                    sortedScores = scoresList.Children.OrderByDescending(x => x.Score.PerformanceAttributes?.Total - x.Score.LivePP).ToArray();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(sortCriteria), sortCriteria, null);
            }

            for (int i = 0; i < sortedScores.Length; i++)
            {
                scoresList.SetLayoutPosition(sortedScores[i], i);
            }
        }
    }
}
