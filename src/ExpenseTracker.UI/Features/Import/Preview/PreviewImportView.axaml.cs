using Avalonia.ReactiveUI;
using ReactiveUI;

namespace ExpenseTracker.UI.Features.Import.Preview;

public partial class PreviewImportView : ReactiveUserControl<PreviewImportViewModel>
{
    public PreviewImportView()
    {
        InitializeComponent();
        this.WhenActivated(_ => { });
    }
}