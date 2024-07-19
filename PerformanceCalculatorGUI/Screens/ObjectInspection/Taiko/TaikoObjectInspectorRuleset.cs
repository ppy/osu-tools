// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Data;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Edit;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.UI;
using osu.Game.Rulesets.UI;
using osuTK.Input;
using PerformanceCalculatorGUI.Screens.ObjectInspection.Osu;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Taiko
{
    public partial class TaikoObjectInspectorRuleset : DrawableTaikoEditorRuleset
    {
        private readonly TaikoDifficultyHitObject[] difficultyHitObjects;

        [Resolved]
        private ObjectDifficultyValuesContainer difficultyValuesContainer { get; set; }

        public TaikoObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedTaikoDifficultyCalculator difficultyCalculator, double clockRate)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate)
                                                       .Cast<TaikoDifficultyHitObject>().ToArray();
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        public override bool AllowBackwardsSeeks => true;


        protected override Playfield CreatePlayfield() => new TaikoObjectInspectorPlayfield();

        }

        private partial class TaikoObjectInspectorPlayfield : TaikoPlayfield
        {
            protected override GameplayCursorContainer CreateCursor() => null;

            public TaikoObjectInspectorPlayfield()
            {
                DisplayJudgements.Value = false;
            }

            private List<TaikoSelectableHitObject> selectables = new();

            protected override void OnHitObjectAdded(HitObject hitObject)
            {
                base.OnHitObjectAdded(hitObject);

                // Potential room for pooling here
                TaikoSelectableHitObject newSelectable = hitObject switch
                {
                    TaikoStrongableHitObject strongable => new TaikoSelectableStrongableHitObject(strongable),
                    TaikoHitObject normal => new TaikoSelectableHitObject(normal),
                    _ => null
                };

                if (newSelectable == null) return;

                HitObjectContainer.Add(newSelectable);
                selectables.Add(newSelectable);
            }

            public readonly Bindable<TaikoHitObject> SelectedObject = new();
            public override bool HandlePositionalInput => true;

            //protected override bool OnClick(ClickEvent e)
            //{
            //    if (e.Button == MouseButton.Right)
            //        return false;

            //    // Variable for handling selection of desired object in the stack (otherwise it will iterate between 2)
            //    //var wasSelectedJustDeselected = false;

            //    TaikoSelectableHitObject newSelectedObject = null;

            //    // This search can be long if list of objects is very big. Potential for optimizing
            //    foreach (var selectable in selectables)
            //    {
            //        if (selectable.IsSelected)
            //        {
            //            selectable.Deselect();
            //            continue;
            //        }

            //        if (!selectable.IsHovered)
            //            continue;

            //        if (newSelectedObject != null)
            //            continue;

            //        selectable.Select();
            //        newSelectedObject = selectable;
            //    }

            //    SelectedObject.Value = newSelectedObject?.HitObject;
            //    return true;
            //}
        }
    }
}
