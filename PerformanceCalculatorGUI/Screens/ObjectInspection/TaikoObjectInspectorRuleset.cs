// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.UI;
using osu.Game.Rulesets.UI;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class TaikoObjectInspectorRuleset : DrawableTaikoRuleset
    {
        private readonly TaikoDifficultyHitObject[] difficultyHitObjects;

        [Resolved]
        private ObjectDifficultyValuesContainer objectDifficultyValuesContainer { get; set; }

        public TaikoObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedTaikoDifficultyCalculator difficultyCalculator, double clockRate)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate)
                                                       .Cast<TaikoDifficultyHitObject>().ToArray();
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        protected override Playfield CreatePlayfield() => new TaikoObjectInspectorPlayfield();

        protected override void Update()
        {
            base.Update();
            objectDifficultyValuesContainer.CurrentDifficultyHitObject.Value = difficultyHitObjects.LastOrDefault(x => x.StartTime < Clock.CurrentTime);
        }

        private partial class TaikoObjectInspectorPlayfield : TaikoPlayfield
        {
            protected override GameplayCursorContainer CreateCursor() => null;

            public TaikoObjectInspectorPlayfield()
            {
                DisplayJudgements.Value = false;
            }
        }
    }
}
