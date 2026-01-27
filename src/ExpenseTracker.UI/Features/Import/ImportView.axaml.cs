using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace ExpenseTracker.UI.Features.Import;

public partial class ImportView : ReactiveUserControl<ImportViewModel>
{
    public ImportView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            if (ViewModel is null) return;

            ViewModel.PickFile.Subscribe(async _ => await PickCsvAsync());
        });
    }

    private void DropZone_OnDragOver(object? sender, DragEventArgs e)
    {
        // 👇 Clean, synchronous check
        var files = e.DataTransfer?.TryGetFiles();
        var hasCsv = files?.Any(f =>
            f.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) == true;

        e.DragEffects = hasCsv
            ? DragDropEffects.Copy
            : DragDropEffects.None;

        e.Handled = true;
    }

    private async void DropZone_OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer?.TryGetFiles();
        if (files is null)
            return;

        var csv = files.FirstOrDefault(f =>
            f.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));

        if (csv is null || ViewModel is null)
            return;

        await ViewModel.LoadFileAsync(csv);

        e.Handled = true;
    }

    private async Task PickCsvAsync()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider is null || ViewModel is null)
            return;

        var results = await top.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Select CSV file",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("CSV files")
                    {
                        Patterns = ["*.csv"]
                    }
                ]
            });

        var file = results.FirstOrDefault();
        if (file is null)
            return;

        await ViewModel.LoadFileAsync(file);
    }
}
