using Avalonia.ReactiveUI;
using ReactiveUI;

namespace ExpenseTracker.UI.Features.Import.PreviewView;

public partial class PreviewImportView : ReactiveUserControl<PreviewImportViewModel>
{
    public PreviewImportView()
    {
        InitializeComponent();
        this.WhenActivated(_ => { });
    }
}