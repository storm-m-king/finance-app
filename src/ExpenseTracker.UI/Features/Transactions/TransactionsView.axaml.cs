using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ExpenseTracker.Infrastructure.Configuration;
using ExpenseTracker.Services.Contracts;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System.IO;

namespace ExpenseTracker.UI.Features.Transactions;

public partial class TransactionsView : ReactiveUserControl<TransactionsViewModel>
{
    private readonly IPlatformService _platformService;

    public TransactionsView()
    {
        InitializeComponent();
        _platformService = Program.Services.GetRequiredService<IPlatformService>();
        this.WhenActivated(_ => { });
    }

    public void CopyDescription(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not TransactionRowViewModel row) return;
        if (!string.IsNullOrEmpty(row.Description))
            _platformService.CopyToClipboard(row.Description);
    }

    public void CopySourceFile(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not TransactionRowViewModel row) return;
        if (!string.IsNullOrEmpty(row.SourceFile))
            _platformService.CopyToClipboard(row.SourceFile);
    }

    public void OpenSourceFileFolder(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not TransactionRowViewModel row) return;
        if (string.IsNullOrEmpty(row.SourceFile)) return;

        var importsDir = AppPaths.GetImportsDirectory();
        var filePath = Path.Combine(importsDir, row.SourceFile);
        _platformService.RevealFileInExplorer(filePath);
    }

    public void CloseNotesFlyout(object? sender, RoutedEventArgs e)
    {
        // Walk the logical tree (Parent) to find the Popup hosting the flyout
        var current = sender as StyledElement;
        while (current != null)
        {
            if (current is Popup popup)
            {
                popup.IsOpen = false;
                return;
            }
            current = current.Parent;
        }
    }
}
