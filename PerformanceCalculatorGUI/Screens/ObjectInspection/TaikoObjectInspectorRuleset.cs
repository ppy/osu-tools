// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty.Preprocessing;
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

        [Resolved]
        private ObjectDifficultyValuesContainer debugValueList { get; set; }

        private DifficultyHitObject lasthit;

        private Bindable<DifficultyHitObject> focusedDiffHitBind;

        public TaikoObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedTaikoDifficultyCalculator difficultyCalculator, double clockRate, Bindable<DifficultyHitObject> diffHitBind)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate)
                                                       .Cast<TaikoDifficultyHitObject>().ToArray();
            focusedDiffHitBind = diffHitBind;
            focusedDiffHitBind.ValueChanged += (ValueChangedEvent<DifficultyHitObject> newHit) => UpdateDebugList(debugValueList, newHit.NewValue);
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        protected override Playfield CreatePlayfield() => new TaikoObjectInspectorPlayfield(difficultyHitObjects);

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

        protected void UpdateDebugList(ObjectDifficultyValuesContainer valueList, DifficultyHitObject curDiffHit)
        {
            if (curDiffHit == null) return;

            TaikoDifficultyHitObject taikoDiffHit = (TaikoDifficultyHitObject)curDiffHit;

            string groupName = taikoDiffHit.BaseObject.GetType().Name;
            valueList.AddGroup(groupName, new string[] { "Hit", "Swell", "DrumRoll" });

            Dictionary<string, Dictionary<string, object>> infoDict = valueList.InfoDictionary.Value;
            infoDict[groupName] = new Dictionary<string, object> {
                { "Delta Time", taikoDiffHit.DeltaTime },
                { "Rhythm Difficulty", taikoDiffHit.Rhythm.Difficulty },
                { "Rhythm Ratio", taikoDiffHit.Rhythm.Ratio }
            };
        }

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
