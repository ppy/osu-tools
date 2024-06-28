using osu.Framework.Input.Events;
using osu.Game.Rulesets.Catch.Edit.Blueprints;
using osu.Game.Rulesets.Catch.Edit;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Spinners;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit.Compose.Components;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.BlueprintContainers
{
    public partial class CatchInspectBlueprintContainer : InspectBlueprintContainer
    {
        public CatchInspectBlueprintContainer(Playfield playfield) : base(playfield)
        {
        }

        protected override HitObjectSelectionBlueprint CreateHitObjectBlueprintFor(HitObject hitObject)
        {
            HitObjectSelectionBlueprint result = hitObject switch
            {
                Fruit fruit => new FruitSelectionBlueprint(fruit),
                JuiceStream juiceStream => new JuiceStreamSelectionBlueprint(juiceStream),
                BananaShower bananaShower => new BananaShowerSelectionBlueprint(bananaShower),
                _ => null
            };

            return result;
        }

        protected override SelectionHandler<HitObject> CreateSelectionHandler() => new CatchSelectionHandler();
    }


}
