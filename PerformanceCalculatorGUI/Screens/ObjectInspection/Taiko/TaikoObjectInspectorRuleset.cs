// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
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
        private TaikoObjectInspectorPlayfield inspectorPlayfield;

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

            inspectorPlayfield.SelectedObject.BindValueChanged(value =>
            {
                difficultyValuesContainer.CurrentDifficultyHitObject.Value = difficultyHitObjects.FirstOrDefault(x => x.BaseObject.StartTime == value.NewValue?.HitObject.StartTime);
            });
        }

        public override bool PropagatePositionalInputSubTree => true;

        public override bool PropagateNonPositionalInputSubTree => false;

        public override bool AllowBackwardsSeeks => true;

        protected override Playfield CreatePlayfield() => inspectorPlayfield = new TaikoObjectInspectorPlayfield();

        protected override PassThroughInputManager CreateInputManager() => new TaikoObjectInspectorInputManager(Ruleset.RulesetInfo);

        private partial class TaikoObjectInspectorInputManager : TaikoInputManager
        {
            public TaikoObjectInspectorInputManager(RulesetInfo ruleset)
                : base(ruleset)
            {
            }

            protected override KeyBindingContainer<TaikoAction> CreateKeyBindingContainer(RulesetInfo ruleset, int variant, SimultaneousBindingMode unique)
                => new EmptyKeyBindingContainer(ruleset, variant, unique);

            private partial class EmptyKeyBindingContainer : RulesetKeyBindingContainer
            {
                public EmptyKeyBindingContainer(RulesetInfo ruleset, int variant, SimultaneousBindingMode unique)
                    : base(ruleset, variant, unique)
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
            public readonly Bindable<TaikoSelectableHitObject> SelectedObject = new Bindable<TaikoSelectableHitObject>();

            public TaikoObjectInspectorPlayfield()
            {
                DisplayJudgements.Value = false;
            }

            protected override void OnHitObjectAdded(HitObject hitObject)
            {
                base.OnHitObjectAdded(hitObject);

                // Potential room for pooling here?
                HitObjectContainer.Add(new TaikoSelectableHitObject((TaikoHitObject)hitObject)
                {
                    PlayfieldSelectedObject = { BindTarget = SelectedObject }
                });
            }

            protected override GameplayCursorContainer CreateCursor() => null;

            protected override bool OnClick(ClickEvent e)
            {
                if (e.Button == MouseButton.Right)
                    return false;

                SelectedObject.Value?.Deselect();
                SelectedObject.Value = null;
                return false;
            }
        }
    }
}
