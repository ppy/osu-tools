using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.Objects.Drawables;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Catch.Edit.Blueprints.Components;
using osuTK;
using osu.Game.Rulesets.Objects.Types;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Catch
{
    public partial class CatchSelectableDrawableObject : DrawableCatchHitObject
    {
        private float x, scale;
        public CatchSelectableDrawableObject(CatchHitObject hitObject)
            : base(new CatchInspectorHitObject(hitObject))
        {
            x = hitObject.EffectiveX;
            scale = hitObject.Scale;

            if (hitObject is Droplet)
                scale *= 0.5f;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(new FruitOutline()
            {
                X = x,
                Scale = new Vector2(scale)
            });
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
