using System;
using Avalonia.Media;
using ReactiveUI;
using ExpenseTracker.UI.ViewModels;

namespace ExpenseTracker.UI.Features.Rules;

public sealed class RuleRowViewModel : ViewModelBase
{
    public Guid Id { get; } = Guid.NewGuid();

    public bool IsDisabled => !IsEnabled;

    private bool _isDropTarget;
    public bool IsDropTarget
    {
        get => _isDropTarget;
        set => this.RaiseAndSetIfChanged(ref _isDropTarget, value);
    }

    private bool _showDropLineAbove;
    public bool ShowDropLineAbove
    {
        get => _showDropLineAbove;
        set => this.RaiseAndSetIfChanged(ref _showDropLineAbove, value);
    }

    private bool _showDropLineBelow;
    public bool ShowDropLineBelow
    {
        get => _showDropLineBelow;
        set => this.RaiseAndSetIfChanged(ref _showDropLineBelow, value);
    }

    private int _order;
    public int Order
    {
        get => _order;
        set => this.RaiseAndSetIfChanged(ref _order, value);
    }

    public string Title { get; }
    public string IfText { get; }
    public string ThenText { get; }

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _isEnabled, value);
            this.RaisePropertyChanged(nameof(IsDisabled));
        }
    }

    // --- Drag visuals ---
    private bool _isDragging;
    public bool IsDragging
    {
        get => _isDragging;
        set
        {
            this.RaiseAndSetIfChanged(ref _isDragging, value);
            this.RaisePropertyChanged(nameof(DragZIndex));
            this.RaisePropertyChanged(nameof(DragOpacity));
        }
    }

    // Draw on top while dragging (even though it stays in the list).
    public int DragZIndex => IsDragging ? 1000 : 0;

    // Lighter while dragging (but still visible).
    public double DragOpacity => IsDragging ? 0.60 : 1.0;

    public void ClearDragHints()
    {
        IsDropTarget = false;
        ShowDropLineAbove = false;
        ShowDropLineBelow = false;
    }

    public RuleRowViewModel(string title, string ifText, string thenText, bool isEnabled)
    {
        Title = title;
        IfText = ifText;
        ThenText = thenText;
        IsEnabled = isEnabled;
    }
}
