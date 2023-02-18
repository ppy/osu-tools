// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.Edit;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class CatchObjectInspectorRuleset : DrawableCatchRuleset
    {
        private readonly CatchDifficultyHitObject[] difficultyHitObjects;

        [Resolved]
        private ObjectDifficultyValuesContainer debugValueList { get; set; }

        private DifficultyHitObject lasthit;

        private Bindable<DifficultyHitObject> focusedDiffHitBind;

        public CatchObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedCatchDifficultyCalculator difficultyCalculator, double clockRate,
                                           Bindable<DifficultyHitObject> diffHitBind)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate)
                                                       .Cast<CatchDifficultyHitObject>().ToArray();
            focusedDiffHitBind = diffHitBind;
            focusedDiffHitBind.ValueChanged += (ValueChangedEvent<DifficultyHitObject> newHit) => UpdateDebugList(debugValueList, newHit.NewValue);
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        protected override Playfield CreatePlayfield() => new CatchObjectInspectorPlayfield(Beatmap.Difficulty);

        protected override void Update()
        {
            base.Update();
            var hitList = difficultyHitObjects.Where(hit => hit.StartTime < Clock.CurrentTime);

            if (hitList.Any() && hitList.Last() != lasthit)
            {
                lasthit = hitList.Last();
                focusedDiffHitBind.Value = lasthit;
            }

            focusedDiffHitBind.Value = null;
        }

        public void UpdateDebugList(ObjectDifficultyValuesContainer valueList, DifficultyHitObject curDiffHit)
        {
            if (curDiffHit == null) return;

            CatchDifficultyHitObject catchDiffHit = (CatchDifficultyHitObject)curDiffHit;

            string groupName = catchDiffHit.BaseObject.GetType().Name;
            valueList.AddGroup(groupName, new string[] { "Fruit", "Droplet" });

            Dictionary<string, Dictionary<string, object>> infoDict = valueList.InfoDictionary.Value;
            infoDict[groupName] = new Dictionary<string, object>
            {
                { "Strain Time", catchDiffHit.StrainTime },
                { "Normalized Position", catchDiffHit.NormalizedPosition },
            };
        }

        private partial class CatchObjectInspectorPlayfield : CatchEditorPlayfield
        {
            protected override GameplayCursorContainer CreateCursor() => null;

            public CatchObjectInspectorPlayfield(IBeatmapDifficultyInfo difficulty)
                : base(difficulty)
            {
                DisplayJudgements.Value = false;
                AddInternal(new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Y = 440,
                    Height = 6.0f,
                    CornerRadius = 4.0f,
                    Masking = true,
                    Child = new Box
                    {
                        Colour = OsuColour.Gray(0.5f),
                        RelativeSizeAxes = Axes.Both
                    }
                });
            }
        }
    }
}
