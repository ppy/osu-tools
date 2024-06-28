using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Screens.Edit.Compose;
using osu.Game.Screens.Edit;
using osu.Game.Rulesets.UI;
using JetBrains.Annotations;
using osu.Framework.Bindables;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection.BlueprintContainers
{
    public abstract partial class InspectBlueprintContainer : BlueprintContainer<HitObject>
    {
        private Playfield playfield;

        [Resolved]
        protected EditorClock EditorClock { get; private set; }

        [Resolved]
        protected EditorBeatmap Beatmap { get; private set; }

        private HitObjectUsageEventBuffer usageEventBuffer;

        protected InputManager InputManager { get; private set; }

        [Cached]
        public readonly Bindable<HitObject> SelectedItem = new();

        public InspectBlueprintContainer(Playfield playfield)
        {
            this.playfield = playfield;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            InputManager = GetContainingInputManager();

            foreach (var obj in playfield.AllHitObjects)
                AddBlueprintFor(obj.HitObject);

            usageEventBuffer = new HitObjectUsageEventBuffer(playfield);
            usageEventBuffer.HitObjectUsageBegan += AddBlueprintFor;
            usageEventBuffer.HitObjectUsageFinished += RemoveBlueprintFor;
            //usageEventBuffer.HitObjectUsageTransferred += TransferBlueprintFor;
        }

        protected override void Update()
        {
            base.Update();
            usageEventBuffer?.Update();
        }

        protected override IEnumerable<SelectionBlueprint<HitObject>> SortForMovement(IReadOnlyList<SelectionBlueprint<HitObject>> blueprints)
            => blueprints.OrderBy(b => b.Item.StartTime);

        protected override bool ApplySnapResult(SelectionBlueprint<HitObject>[] blueprints, SnapResult result)
        {
            if (!base.ApplySnapResult(blueprints, result))
                return false;

            if (result.Time.HasValue)
            {
                // Apply the start time at the newly snapped-to position
                var offset = result.Time.Value - blueprints.First().Item.StartTime;

                if (offset != 0)
                {
                    Beatmap.PerformOnSelection(obj =>
                    {
                        obj.StartTime += offset;
                        Beatmap.Update(obj);
                    });
                }
            }

            return true;
        }

        protected override void AddBlueprintFor(HitObject item)
        {
            if (item is IBarLine)
                return;

            base.AddBlueprintFor(item);
        }

        protected sealed override SelectionBlueprint<HitObject> CreateBlueprintFor(HitObject item)
        {
            
            var drawable = playfield.AllHitObjects.FirstOrDefault(d => d.HitObject == item);

            if (drawable == null)
                return null;

            return CreateHitObjectBlueprintFor(item)?.With(b => b.DrawableObject = drawable);
        }

        [CanBeNull]
        protected abstract HitObjectSelectionBlueprint CreateHitObjectBlueprintFor(HitObject hitObject);

        //protected virtual void TransferBlueprintFor(HitObject hitObject, DrawableHitObject drawableObject)
        //{
        //}

        protected override bool OnDragStart(DragStartEvent e) => false;
        protected override void OnDrag(DragEvent e) { }
        protected override void OnDragEnd(DragEndEvent e) { }

        protected override bool OnDoubleClick(DoubleClickEvent e)
        {
            if (!base.OnDoubleClick(e))
                return false;

            EditorClock?.SeekSmoothlyTo(ClickedBlueprint.Item.StartTime);
            return true;
        }

        protected override IEnumerable<SelectionBlueprint<HitObject>> ApplySelectionOrder(IEnumerable<SelectionBlueprint<HitObject>> blueprints) =>
            base.ApplySelectionOrder(blueprints)
                .OrderBy(b => Math.Min(Math.Abs(EditorClock.CurrentTime - b.Item.GetEndTime()), Math.Abs(EditorClock.CurrentTime - b.Item.StartTime)));

        protected override Container<SelectionBlueprint<HitObject>> CreateSelectionBlueprintContainer() => new HitObjectOrderedSelectionContainer { RelativeSizeAxes = Axes.Both };

        protected override SelectionHandler<HitObject> CreateSelectionHandler() => new InspectSelectionHandler();

        protected override void SelectAll() { }

        protected override void OnBlueprintSelected(SelectionBlueprint<HitObject> blueprint)
        {
            base.OnBlueprintSelected(blueprint);

            playfield.SetKeepAlive(blueprint.Item, true);
        }

        protected override void OnBlueprintDeselected(SelectionBlueprint<HitObject> blueprint)
        {
            base.OnBlueprintDeselected(blueprint);

            playfield.SetKeepAlive(blueprint.Item, false);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            usageEventBuffer?.Dispose();
        }

        private partial class InspectSelectionHandler : SelectionHandler<HitObject>
        {
            [Resolved]
            private Bindable<HitObject> selectedItem { get; set; }

            protected override void OnSelectionChanged()
            {
                selectedItem.Value = SelectedItems.FirstOrDefault();
                SelectionBox.Hide();
            }
            protected override void DeleteItems(IEnumerable<HitObject> items) { }
        }
    }
}
