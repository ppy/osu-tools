// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Objects.Drawables;
using osu.Game.Rulesets.Taiko.UI;
using osu.Game.Rulesets.UI;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class TaikoObjectInspectorRuleset : DrawableTaikoRuleset
    {
        private readonly TaikoDifficultyHitObject[] difficultyHitObjects;

        public TaikoObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedTaikoDifficultyCalculator difficultyCalculator, double clockRate)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate)
                                                       .Select(x => (TaikoDifficultyHitObject)x).ToArray();
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        protected override Playfield CreatePlayfield() => new TaikoObjectInspectorPlayfield(difficultyHitObjects);

        private partial class TaikoObjectInspectorPlayfield : TaikoPlayfield
        {
            private readonly IReadOnlyList<TaikoDifficultyHitObject> difficultyHitObjects;

            protected override GameplayCursorContainer CreateCursor() => null;

            public TaikoObjectInspectorPlayfield(IReadOnlyList<TaikoDifficultyHitObject> difficultyHitObjects)
            {
                this.difficultyHitObjects = difficultyHitObjects;
                DisplayJudgements.Value = false;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                foreach (var dho in difficultyHitObjects)
                {
                    HitObjectContainer.Add(new TaikoInspectorDrawableHitObject(dho));
                }
            }

            private partial class TaikoInspectorDrawableHitObject : DrawableTaikoHitObject
            {
                private readonly TaikoDifficultyHitObject dho;

                public TaikoInspectorDrawableHitObject(TaikoDifficultyHitObject dho)
                    : base(new TaikoInspectorHitObject(dho.BaseObject))
                {
                    this.dho = dho;
                }

                [BackgroundDependencyLoader]
                private void load()
                {
                    ObjectInspectionPanel panel;
                    AddInternal(panel = new ObjectInspectionPanel());

                    panel.AddParagraph($"Delta Time: {dho.DeltaTime:N3}");
                    panel.AddParagraph($"Rhythm Difficulty: {dho.Rhythm.Difficulty:N3}");
                    panel.AddParagraph($"Rhythm Ratio: {dho.Rhythm.Ratio:N3}");
                }

                public override bool OnPressed(KeyBindingPressEvent<TaikoAction> e) => true;

                private class TaikoInspectorHitObject : TaikoHitObject
                {
                    public TaikoInspectorHitObject(HitObject obj)
                    {
                        StartTime = obj.StartTime;
                    }
                }
            }
        }
    }
}
