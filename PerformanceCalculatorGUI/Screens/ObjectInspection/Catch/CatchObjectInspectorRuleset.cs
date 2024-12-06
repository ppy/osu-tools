// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.Edit;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UI;
using osuTK.Input;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Catch
{
    public partial class CatchObjectInspectorRuleset : DrawableCatchEditorRuleset
    {
        private readonly CatchDifficultyHitObject[] difficultyHitObjects;
        private CatchObjectInspectorPlayfield inspectorPlayfield;

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

            inspectorPlayfield.SelectedObject.BindValueChanged(value =>
            {
                difficultyValuesContainer.CurrentDifficultyHitObject.Value = difficultyHitObjects.FirstOrDefault(x => x.BaseObject.StartTime == value.NewValue?.HitObject.StartTime);
            });
        }

        public override bool PropagatePositionalInputSubTree => true;

        public override bool PropagateNonPositionalInputSubTree => false;

        public override bool AllowBackwardsSeeks => true;

        protected override Playfield CreatePlayfield() => inspectorPlayfield = new CatchObjectInspectorPlayfield(Beatmap.Difficulty);

        private partial class CatchObjectInspectorPlayfield : CatchEditorPlayfield
        {
            public readonly Bindable<CatchSelectableHitObject?> SelectedObject = new Bindable<CatchSelectableHitObject?>();

            public CatchObjectInspectorPlayfield(IBeatmapDifficultyInfo difficulty)
                : base(difficulty)
            {
                DisplayJudgements.Value = false;
            }

            protected override void OnHitObjectAdded(HitObject hitObject)
            {
                base.OnHitObjectAdded(hitObject);

                // Potential room for pooling here?
                switch (hitObject)
                {
                    case Fruit fruit:
                    {
                        HitObjectContainer.Add(new CatchSelectableHitObject(fruit)
                        {
                            PlayfieldSelectedObject = { BindTarget = SelectedObject }
                        });

                        break;
                    }

                    case JuiceStream juiceStream:
                    {
                        foreach (var nested in juiceStream.NestedHitObjects)
                        {
                            if (nested is TinyDroplet)
                                continue;

                            HitObjectContainer.Add(new CatchSelectableHitObject((CatchHitObject)nested)
                            {
                                PlayfieldSelectedObject = { BindTarget = SelectedObject }
                            });
                        }

                        break;
                    }
                }
            }

            protected override GameplayCursorContainer CreateCursor() => null!;

            protected override bool OnClick(ClickEvent e)
            {
                if (e.Button == MouseButton.Left)
                {
                    SelectedObject.Value?.Deselect();
                    SelectedObject.Value = null;
                }

                return false;
            }
        }
    }
}
