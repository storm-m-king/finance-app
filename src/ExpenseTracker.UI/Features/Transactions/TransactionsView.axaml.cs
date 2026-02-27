using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ExpenseTracker.UI.Features.Transactions;

public partial class TransactionsView : ReactiveUserControl<TransactionsViewModel>
{
    public TransactionsView()
    {
        InitializeComponent();
        this.WhenActivated(_ => { });
    }

    public void CopyDescription(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not TransactionRowViewModel row) return;
        CopyTextToClipboard(row.Description);
    }

    public void CopySourceFile(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not TransactionRowViewModel row) return;
        CopyTextToClipboard(row.SourceFile);
    }

    public void OpenSourceFileFolder(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not TransactionRowViewModel row) return;
        if (string.IsNullOrEmpty(row.SourceFile)) return;

        var importsDir = ExpenseTracker.Infrastructure.Configuration.AppPaths.GetImportsDirectory();
        var filePath = Path.Combine(importsDir, row.SourceFile);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (File.Exists(filePath))
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            else
                Process.Start("explorer.exe", importsDir);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (File.Exists(filePath))
                Process.Start("open", $"-R \"{filePath}\"");
            else
                Process.Start("open", importsDir);
        }
        else
        {
            Process.Start("xdg-open", importsDir);
        }
    }

    private static void CopyTextToClipboard(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;

        string command;
        string args;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            command = "clip.exe";
            args = "";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            command = "pbcopy";
            args = "";
        }
        else
        {
            command = "xclip";
            args = "-selection clipboard";
        }

        var psi = new ProcessStartInfo(command, args)
        {
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi);
        if (process != null)
        {
            process.StandardInput.Write(text);
            process.StandardInput.Close();
        }
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
