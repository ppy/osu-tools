// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.UI;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class OsuObjectInspectorRuleset : DrawableOsuRuleset
    {
        private readonly OsuDifficultyHitObject[] difficultyHitObjects;

        [Resolved]
        private ObjectDifficultyValuesContainer objectDifficultyValuesContainer { get; set; }

        public OsuObjectInspectorRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, ExtendedOsuDifficultyCalculator difficultyCalculator, double clockRate)
            : base(ruleset, beatmap, mods)
        {
            difficultyHitObjects = difficultyCalculator.GetDifficultyHitObjects(beatmap, clockRate).Cast<OsuDifficultyHitObject>().ToArray();
        }

        protected override void Update()
        {
            base.Update();
            objectDifficultyValuesContainer.CurrentDifficultyHitObject.Value = difficultyHitObjects.LastOrDefault(x => x.StartTime < Clock.CurrentTime);
        }

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        protected override Playfield CreatePlayfield() => new OsuObjectInspectorPlayfield();

        private partial class OsuObjectInspectorPlayfield : OsuPlayfield
        {
            protected override GameplayCursorContainer CreateCursor() => null;

            public OsuObjectInspectorPlayfield()
            {
                HitPolicy = new AnyOrderHitPolicy();
                DisplayJudgements.Value = false;
            }
        }
    }
}
