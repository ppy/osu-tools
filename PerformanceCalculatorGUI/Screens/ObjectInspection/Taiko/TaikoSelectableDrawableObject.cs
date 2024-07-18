#nullable enable

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Taiko.Edit.Blueprints;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Objects.Drawables;
using osu.Game.Rulesets.Taiko.UI;
using osuTK;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.Taiko
{
    public partial class TaikoSelectableDrawableObject : DrawableTaikoHitObject
    {
        public TaikoSelectableDrawableObject(TaikoHitObject hitObject) : base(new TaikoInspectorHitObject(hitObject))
        {
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(new HitPiece() {Size = GetObjectSize() });
        }

        protected virtual Vector2 GetObjectSize() => new Vector2(TaikoHitObject.DEFAULT_SIZE * TaikoPlayfield.BASE_HEIGHT);

        public override bool OnPressed(KeyBindingPressEvent<TaikoAction> e) => true;
    }

    public class TaikoInspectorHitObject : TaikoHitObject
    {
        public TaikoInspectorHitObject(HitObject obj)
        {
            StartTime = obj.StartTime;
        }
    }
}
