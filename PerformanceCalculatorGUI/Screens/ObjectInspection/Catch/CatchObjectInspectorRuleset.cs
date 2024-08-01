// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Input.Bindings;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.Edit;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UI;
using osuTK.Input;
using PerformanceCalculatorGUI.Screens.ObjectInspection.Taiko;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Catch
{
    public partial class CatchObjectInspectorRuleset : DrawableCatchEditorRuleset
    {
        private readonly CatchDifficultyHitObject[] difficultyHitObjects;

        [Resolved]
        private ObjectDifficultyValuesContainer difficultyValuesContainer { get; set; }

        public CatchObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedCatchDifficultyCalculator difficultyCalculator, double clockRate)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate)
                                                       .Cast<CatchDifficultyHitObject>().ToArray();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            ((CatchObjectInspectorPlayfield)Playfield).SelectedObject.BindValueChanged(
                value => difficultyValuesContainer.CurrentDifficultyHitObject.Value = difficultyHitObjects.FirstOrDefault(x => x.BaseObject.StartTime == value.NewValue?.StartTime));
        }

        public override bool PropagatePositionalInputSubTree => true;

        public override bool PropagateNonPositionalInputSubTree => false;

        public override bool AllowBackwardsSeeks => true;

        protected override PassThroughInputManager CreateInputManager() => new CatchObjectInspectorInputManager(Ruleset.RulesetInfo);

        protected override Playfield CreatePlayfield() => new CatchObjectInspectorPlayfield(Beatmap.Difficulty);

        private partial class CatchObjectInspectorInputManager : CatchInputManager
        {
            public CatchObjectInspectorInputManager(RulesetInfo ruleset) : base(ruleset)
            {
            }

            protected override KeyBindingContainer<CatchAction> CreateKeyBindingContainer(RulesetInfo ruleset, int variant, SimultaneousBindingMode unique)
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

        private partial class CatchObjectInspectorPlayfield : CatchEditorPlayfield
        {
            public readonly Bindable<CatchHitObject> SelectedObject = new();

            private CatchSelectableHitObject selectedSelectableObject;

            public CatchObjectInspectorPlayfield(IBeatmapDifficultyInfo difficulty)
                : base(difficulty)
            {
                DisplayJudgements.Value = false;
            }

            private DrawablePool<CatchSelectableHitObject> selectablesPool;

            [BackgroundDependencyLoader]
            private void load()
            {
                AddInternal(selectablesPool = new DrawablePool<CatchSelectableHitObject>(1, 200));
            }

            protected override void OnHitObjectAdded(HitObject hitObject)
            {
                base.OnHitObjectAdded(hitObject);

                // Potential room for pooling here
                switch (hitObject)
                {
                    case Fruit fruit:
                    {
                        var newSelectable = selectablesPool.Get(a => a.UpdateFromHitObject((CatchHitObject)hitObject));
                        HitObjectContainer.Add(newSelectable);
                        newSelectable.Selected += selectNewObject;
                        break;
                    }
                    case JuiceStream juiceStream:
                    {
                        foreach (var nested in juiceStream.NestedHitObjects)
                        {
                            if (nested is TinyDroplet)
                                continue;

                            var newSelectable = selectablesPool.Get(a => a.UpdateFromHitObject((CatchHitObject)nested));
                            HitObjectContainer.Add(newSelectable);
                            newSelectable.Selected += selectNewObject;
                        }
                        break;
                    }
                }
            }

            protected override GameplayCursorContainer CreateCursor() => null;

            private void selectNewObject(CatchSelectableHitObject newSelectable)
            {
                selectedSelectableObject?.Deselect();
                selectedSelectableObject = newSelectable;
                SelectedObject.Value = newSelectable?.HitObject;
            }

            protected override bool OnClick(ClickEvent e)
            {
                if (e.Button == MouseButton.Right)
                    return false;

                selectNewObject(null);
                return false;
            }
        }
    }
}
