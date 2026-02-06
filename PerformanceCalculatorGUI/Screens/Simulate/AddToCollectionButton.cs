// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.UserInterfaceV2;
using PerformanceCalculatorGUI.Screens.Collections;

namespace PerformanceCalculatorGUI.Screens.Simulate
{
    public partial class AddToCollectionButton : Container
    {
        private readonly RoundedButton addButton;
        private readonly FillFlowContainer collectionListContainer;
        private readonly FillFlowContainer collectionList;
        private readonly CreateCollectionButton createCollectionButton;

        public delegate void OnAddHandler(Collection collection);

        public event OnAddHandler? OnAdd;

        private const int button_height = 40;
        private const int fade_duration = 200;
        private const string collections_directory = "collections";

        private readonly Bindable<Collection?> selectedCollection = new Bindable<Collection?>();

        public AddToCollectionButton()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Children = new Drawable[]
            {
                addButton = new RoundedButton
                {
                    RelativeSizeAxes = Axes.X,
                    Text = "Add to Collection",
                    Height = button_height,
                    Action = showSelection
                },
                collectionListContainer = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new osuTK.Vector2(0, 2f),
                    Alpha = 0,
                    Children = new Drawable[]
                    {
                        collectionList = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                        },
                        createCollectionButton = new CreateCollectionButton()
                    }
                }
            };

            selectedCollection.ValueChanged += e =>
            {
                if (e.NewValue == null)
                    return;

                collectionListContainer.FadeOut(fade_duration);
                addButton.FadeIn(fade_duration);

                OnAdd?.Invoke(e.NewValue);

                selectedCollection.Value = null;
            };

            createCollectionButton.OnSave += onCollectionCreated;
        }

        private void showSelection()
        {
            loadCollectionList();

            collectionListContainer.FadeIn(fade_duration);
            addButton.FadeOut(fade_duration);
        }

        private void loadCollectionList()
        {
            collectionList.Clear();

            if (!Directory.Exists(collections_directory))
            {
                Directory.CreateDirectory(collections_directory);
                return;
            }

            var collections = new List<Collection>();

            foreach (string collectionFile in Directory.EnumerateFiles(collections_directory))
            {
                var collection = JsonConvert.DeserializeObject<Collection>(File.ReadAllText(collectionFile));

                if (collection != null)
                    collections.Add(collection);
            }

            foreach (var collection in collections.OrderBy(x => x.Name))
            {
                collectionList.Add(new CollectionButton(collection, selectedCollection));
            }
        }

        private void onCollectionCreated(string name)
        {
            string fileName = RandomNumberGenerator.GetString(choices: "abcdefghijklmnopqrstuvwxyz0123456789", length: 16) + ".json";

            var collection = new Collection
            {
                Name = name,
                FileName = fileName,
                Scores = []
            };

            if (!Directory.Exists(collections_directory))
                Directory.CreateDirectory(collections_directory);

            string path = Path.Combine(collections_directory, fileName);
            File.WriteAllText(path, JsonConvert.SerializeObject(collection));

            loadCollectionList();
        }
    }
}
