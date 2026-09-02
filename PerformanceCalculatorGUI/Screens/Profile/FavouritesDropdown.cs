// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Graphics.UserInterface;

namespace PerformanceCalculatorGUI.Screens.Profile
{
    public partial class FavouritesDropdown : OsuDropdown<string?>
    {
        public Bindable<string[]> CurrentUsers = null!;

        private readonly Bindable<string?> favouritesSelectionBindable = new Bindable<string?>();
        private List<string> favourites = new List<string>();

        private const string favourites_file = "favourites.json";

        private const string default_item = "Favourites";
        private const string add_current_item = "Add current...";

        public FavouritesDropdown()
        {
            Current = favouritesSelectionBindable;
            AlwaysShowSearchBar = true;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            loadFavourites();
            favouritesSelectionBindable.ValueChanged += onSelection;
        }

        private void onSelection(ValueChangedEvent<string?> e)
        {
            if (e.NewValue == null || e.NewValue == default_item)
                return;

            if (e.NewValue == add_current_item)
            {
                favourites.AddRange(CurrentUsers.Value);
                saveFavourites(favourites);
                updateItems();
            }
            else
            {
                CurrentUsers.Value = [e.NewValue];
            }

            favouritesSelectionBindable.Value = default_item;
        }

        private void loadFavourites()
        {
            if (File.Exists(favourites_file))
            {
                string[]? deserialized = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(favourites_file));

                if (deserialized != null)
                {
                    favourites = deserialized.ToList();
                }
            }

            updateItems();
            favouritesSelectionBindable.Value = default_item;
        }

        private void updateItems()
        {
            var items = new List<string> { add_current_item };
            items.InsertRange(0, favourites);

            Items = items;
        }

        private void saveFavourites(List<string> list)
        {
            File.WriteAllText(favourites_file, JsonConvert.SerializeObject(list));
        }
    }
}
