// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Data;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Input.Bindings;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Edit;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.UI;
using osu.Game.Rulesets.UI;
using osuTK.Input;

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

        protected override void LoadComplete()
        {
            base.LoadComplete();
            ((TaikoObjectInspectorPlayfield)Playfield).SelectedObject.BindValueChanged(
                value => difficultyValuesContainer.CurrentDifficultyHitObject.Value = difficultyHitObjects.FirstOrDefault(x => x.BaseObject.StartTime == value.NewValue?.StartTime));
        }

        public override bool PropagatePositionalInputSubTree => true;

        public override bool PropagateNonPositionalInputSubTree => false;

        public override bool AllowBackwardsSeeks => true;

        protected override PassThroughInputManager CreateInputManager() => new TaikoObjectInspectorInputManager(Ruleset.RulesetInfo);

        protected override Playfield CreatePlayfield() => new TaikoObjectInspectorPlayfield();

        private partial class TaikoObjectInspectorInputManager : TaikoInputManager
        {
            public TaikoObjectInspectorInputManager(RulesetInfo ruleset) : base(ruleset)
            {
            }

            protected override KeyBindingContainer<TaikoAction> CreateKeyBindingContainer(RulesetInfo ruleset, int variant, SimultaneousBindingMode unique)
            => new EmptyKeyBindingContainer(ruleset, variant, unique);

            private partial class EmptyKeyBindingContainer : RulesetKeyBindingContainer
            {
                public EmptyKeyBindingContainer(RulesetInfo ruleset, int variant, SimultaneousBindingMode unique) : base(ruleset, variant, unique)
                {
                }

                protected override void ReloadMappings(IQueryable<RealmKeyBinding> realmKeyBindings)
                {
                    base.ReloadMappings(realmKeyBindings);
                    KeyBindings = Enumerable.Empty<IKeyBinding>();
                }
            }
        }

        private partial class TaikoObjectInspectorPlayfield : TaikoPlayfield
        {
            public readonly Bindable<TaikoHitObject> SelectedObject = new();

            private List<TaikoSelectableHitObject> selectables = new();

            public TaikoObjectInspectorPlayfield()
            {
                DisplayJudgements.Value = false;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                AddRangeInternal(new Drawable[]
                {
                    strongablesPool = new DrawablePool<TaikoSelectableStrongableHitObject>(1, 200),
                    normalsPool = new DrawablePool<TaikoSelectableHitObject>(1, 200)
                });
            }

            private DrawablePool<TaikoSelectableStrongableHitObject> strongablesPool;
            private DrawablePool<TaikoSelectableHitObject> normalsPool;

            protected override void OnHitObjectAdded(HitObject hitObject)
            {
                base.OnHitObjectAdded(hitObject);

                // Potential room for pooling here
                TaikoSelectableHitObject newSelectable = hitObject switch
                {
                    TaikoStrongableHitObject => strongablesPool.Get(),
                    TaikoHitObject => normalsPool.Get(),
                    _ => null
                };

                if (newSelectable == null) return;

                newSelectable.UpdateFromHitObject((TaikoHitObject)hitObject);
                HitObjectContainer.Add(newSelectable);
                selectables.Add(newSelectable);
            }

            protected override GameplayCursorContainer CreateCursor() => null;

            public override bool HandlePositionalInput => true;

            protected override bool OnClick(ClickEvent e)
            {
                if (e.Button == MouseButton.Right)
                    return false;

                TaikoSelectableHitObject newSelectedObject = null;

                // This search can be long if list of objects is very big. Potential for optimization
                foreach (var selectable in selectables)
                {
                    if (selectable.IsSelected)
                    {
                        selectable.Deselect();
                        continue;
                    }

                    if (!selectable.IsHovered)
                        continue;

                    if (newSelectedObject != null)
                        continue;

                    selectable.Select();
                    newSelectedObject = selectable;
                }

                SelectedObject.Value = newSelectedObject?.HitObject;
                return true;
            }
        }
    }
}
