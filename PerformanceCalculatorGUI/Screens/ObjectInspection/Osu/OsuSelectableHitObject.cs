using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Screens.Edit;
using TagLib.Ape;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public abstract partial class OsuSelectableHitObject<THitObject> : SelectableHitObject<THitObject>
        where THitObject : OsuHitObject
    {
        //protected override bool ShouldBeAlive => base.ShouldBeAlive
        //                                         || (DrawableObject is not DrawableSpinner && ShowHitMarkers.Value && editorClock.CurrentTime >= Item.StartTime
        //                                             && editorClock.CurrentTime - Item.GetEndTime() < HitCircleOverlapMarker.FADE_OUT_EXTENSION);
    }
}
