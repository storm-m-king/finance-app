using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace ExpenseTracker.UI.Features.Rules;

public partial class RulesView : UserControl
{
    private RuleRowViewModel? _dragging;

    public RulesView()
    {
        InitializeComponent();

        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleasedAnywhere, RoutingStrategies.Tunnel);
    }

    private void DragHandle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control handle)
            return;

        var rowBorder = handle.FindAncestorOfType<Border>();
        if (rowBorder?.Tag is not RuleRowViewModel rule)
            return;

        if (DataContext is not RulesViewModel vm)
            return;

        // Start drag
        _dragging = rule;
        _dragging.IsDragging = true;

        // Clear hints initially
        vm.ClearAllDragHints();

        // Capture so we can't "accidentally pick up" another card.
        e.Pointer.Capture(handle);
        e.Handled = true;
    }

    private void DragHandle_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        => EndDrag(e.Pointer);

    private void OnPointerReleasedAnywhere(object? sender, PointerReleasedEventArgs e)
        => EndDrag(e.Pointer);

    private void EndDrag(IPointer pointer)
    {
        if (_dragging == null)
            return;

        pointer.Capture(null);

        if (DataContext is RulesViewModel vm)
            vm.ClearAllDragHints();

        _dragging.IsDragging = false;
        _dragging = null;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragging == null)
            return;

        if (DataContext is not RulesViewModel vm)
            return;

        // We compute target index using pointer Y relative to the ItemsControl
        var p = e.GetPosition(RulesList);

        // Clamp to list bounds so we don't drag outside the list area
        var y = Math.Clamp(p.Y, 0, Math.Max(0, RulesList.Bounds.Height));

        var containers = RulesList
            .GetVisualDescendants()
            .OfType<Border>()
            .Where(b => b.Tag is RuleRowViewModel)
            .ToList();

        if (containers.Count == 0)
            return;

        // Find which card we are "over" and whether we are in its top/bottom half
        Border? overBorder = null;
        RuleRowViewModel? overVm = null;
        bool inTopHalf = false;

        foreach (var b in containers)
        {
            var topLeft = b.TranslatePoint(new Avalonia.Point(0, 0), RulesList);
            if (topLeft is null)
                continue;

            var top = topLeft.Value.Y;
            var bottom = top + b.Bounds.Height;

            if (y >= top && y <= bottom)
            {
                overBorder = b;
                overVm = (RuleRowViewModel)b.Tag!;
                var mid = top + (b.Bounds.Height / 2);
                inTopHalf = y < mid;
                break;
            }
        }

        // If not directly over any card, we treat it as top/bottom insertion.
        int targetIndex;
        if (overVm != null)
        {
            var overIndex = vm.IndexOf(overVm);
            targetIndex = inTopHalf ? overIndex : overIndex + 1;
        }
        else
        {
            // above first or below last
            targetIndex = (y <= 0) ? 0 : vm.Rules.Count;
        }

        // Snap-to-slot reorder (move dragged item to targetIndex-1 logic handled inside)
        vm.ClearAllDragHints();

        // Show drop hints on the hovered card (not on the dragged one)
        if (overVm != null && overVm != _dragging)
        {
            overVm.IsDropTarget = true;
            overVm.ShowDropLineAbove = inTopHalf;
            overVm.ShowDropLineBelow = !inTopHalf;
        }

        // Move the dragged item in the collection
        vm.MoveRuleToIndex(_dragging, targetIndex);
    }
}
