// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.Edit;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Objects.Drawables;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UI;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class CatchObjectInspectorRuleset : DrawableCatchRuleset
    {

        private readonly CatchDifficultyHitObject[] difficultyHitObjects;
        [Resolved]
        private ObjectDifficultyValuesContainer debugValueList { get; set; }

        private DifficultyHitObject lasthit;

        public CatchObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedCatchDifficultyCalculator difficultyCalculator, double clockRate)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate)
                                                       .Select(x => (CatchDifficultyHitObject)x).ToArray();
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;


        protected override Playfield CreatePlayfield() => new CatchObjectInspectorPlayfield(Beatmap.Difficulty, difficultyHitObjects);

        protected override void Update()
        {
            base.Update();
            var returnedhit = ObjectInspector.GetCurrentHit(Playfield, difficultyHitObjects, lasthit, Clock);
            if (returnedhit != null)
            {
                lasthit = returnedhit;
                UpdateDebugList(debugValueList, lasthit);
            }
        }

        public void UpdateDebugList(ObjectDifficultyValuesContainer valueList, DifficultyHitObject curDiffHit)
        {
            CatchDifficultyHitObject catchDiffHit = (CatchDifficultyHitObject)curDiffHit;
            string groupName = catchDiffHit.BaseObject.GetType().Name;
            valueList.AddGroup(groupName, new string[] { "Fruit", "Droplet" });
            valueList.SetValue(groupName, "Strain Time", catchDiffHit.StrainTime);
            valueList.SetValue(groupName, "Normalized Position", catchDiffHit.NormalizedPosition);
            valueList.UpdateValues();
        }

        private partial class CatchObjectInspectorPlayfield : CatchEditorPlayfield
        {
            private readonly IReadOnlyList<CatchDifficultyHitObject> difficultyHitObjects;

            protected override GameplayCursorContainer CreateCursor() => null;

            public CatchObjectInspectorPlayfield(IBeatmapDifficultyInfo difficulty, IReadOnlyList<CatchDifficultyHitObject> difficultyHitObjects)
                : base(difficulty)
            {
                this.difficultyHitObjects = difficultyHitObjects;
                DisplayJudgements.Value = false;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                foreach (var dho in difficultyHitObjects)
                {
                    HitObjectContainer.Add(new CatchInspectorDrawableHitObject(dho));
                }
            }

            private partial class CatchInspectorDrawableHitObject : DrawableCatchHitObject
            {
                private readonly CatchDifficultyHitObject dho;

                public CatchInspectorDrawableHitObject(CatchDifficultyHitObject dho)
                    : base(new CatchInspectorHitObject(dho.BaseObject))
                {
                    this.dho = dho;
                }

                [BackgroundDependencyLoader]
                private void load()
                {
                }

                private class CatchInspectorHitObject : CatchHitObject
                {
                    public CatchInspectorHitObject(HitObject obj)
                    {
                        StartTime = obj.StartTime;
                    }
                }
            }
        }
    }
}
