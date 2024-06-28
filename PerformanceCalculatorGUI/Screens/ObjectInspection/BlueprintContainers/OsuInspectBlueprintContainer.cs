using osu.Framework.Input.Events;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Spinners;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.UI;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.BlueprintContainers
{
    public partial class OsuInspectBlueprintContainer : InspectBlueprintContainer
    {
        public OsuInspectBlueprintContainer(Playfield playfield) : base(playfield)
        {
        }

        protected override HitObjectSelectionBlueprint CreateHitObjectBlueprintFor(HitObject hitObject)
        {
            HitObjectSelectionBlueprint result = hitObject switch
            {
                HitCircle circle => new HitCircleSelectionBlueprint(circle),
                Slider slider => new UneditableSliderSelectionBlueprint(slider),
                Spinner spinner => new SpinnerSelectionBlueprint(spinner),
                _ => null
            };

            return result;
        }

        private partial class UneditableSliderSelectionBlueprint(Slider slider) : SliderSelectionBlueprint(slider)
        {
            protected override bool OnMouseDown(MouseDownEvent e) => false;
            protected override void UpdateVisualDefinition() { }
        }
    }

    
}
