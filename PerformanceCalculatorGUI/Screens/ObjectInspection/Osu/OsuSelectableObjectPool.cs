using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Pooling;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Spinners;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Osu
{
    public partial class OsuSelectableObjectPool : SelectableObjectPool
    {
        //private DrawablePool<SelectableHitCircle> circlesPool;

        //[BackgroundDependencyLoader]
        //private void load()
        //{
        //    InternalChildren = new Drawable[]
        //    {
        //        circlesPool = new(1)
        //    };
        //}

        //circlesPool.Get().With(o => o.HitObject = circle)

        public override SelectableObjectLifetimeEntry CreateEntry(HitObject hitObject) => new OsuSelectableObjectLifetimeEntry((OsuHitObject)hitObject);

        protected override SelectableHitObject GetDrawable(SelectableObjectLifetimeEntry entry)
        {
            SelectableHitObject result = entry.HitObject switch
            {
                HitCircle circle => new SelectableHitCircle().With(o => o.HitObject = circle),
                Slider slider => null,
                Spinner spinner => null,
                _ => null
            };

            return result;
        }
    }
}
