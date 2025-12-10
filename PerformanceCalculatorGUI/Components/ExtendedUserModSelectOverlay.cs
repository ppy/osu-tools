// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Overlays;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.Mods;

namespace PerformanceCalculatorGUI.Components
{
    public partial class ExtendedUserModSelectOverlay : UserModSelectOverlay
    {
        public ExtendedUserModSelectOverlay()
            : base(OverlayColourScheme.Blue)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var method = typeof(ModSelectOverlay).GetMethod("createModColumnContent", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null) return;

            object systemColumn = method.Invoke(this, new object[] { ModType.System });
            if (systemColumn == null) return;

            var flowField = typeof(ModSelectOverlay).GetField("columnFlow",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (flowField == null) return;

            object columnFlow = flowField.GetValue(this);
            if (columnFlow == null) return;

            var addMethod = columnFlow.GetType().GetMethod("Add");
            if (addMethod == null) return;

            addMethod.Invoke(columnFlow, new[] { systemColumn });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            foreach (var modStates in AvailableMods.Value)
            {
                foreach (var modState in modStates.Value)
                {
                    if (modState.Mod.Type == ModType.System)
                        modState.ValidForSelection.Value = true;
                }
            }
        }

        protected override void PopIn()
        {
            Header.Hide();
            MainAreaContent.Padding = new MarginPadding();
            TopLevelContent.Children[0].Hide(); // hide the gray background of the ShearedOverlayContainer

            base.PopIn();
        }
    }
}
