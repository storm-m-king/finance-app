using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Platform.Storage;
using ReactiveUI;
using ExpenseTracker.UI.ViewModels;

namespace ExpenseTracker.UI.Features.Import;

public sealed class ImportViewModel : ViewModelBase
{
    public ObservableCollection<string> MappingProfiles { get; } = new()
    {
        "",
        "Amex",
        "SoFi Checking",
        "Capital One",
        "Chase"
    };

    private string? _selectedMappingProfile;
    public string? SelectedMappingProfile
    {
        get => _selectedMappingProfile;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMappingProfile, value);
            this.RaisePropertyChanged(nameof(CanContinue));
        }
    }

    private string? _selectedFileName;
    public string? SelectedFileName
    {
        get => _selectedFileName;
        private set
        {
            this.RaiseAndSetIfChanged(ref _selectedFileName, value);
            this.RaisePropertyChanged(nameof(HasSelectedFile));
            this.RaisePropertyChanged(nameof(CanContinue));
        }
    }

    private string? _selectedFileAbsolutePath;
    public string? SelectedFileAbsolutePath
    {
        get => _selectedFileAbsolutePath;
        private set => this.RaiseAndSetIfChanged(ref _selectedFileAbsolutePath, value);
    }
    
    private ImportFile? _selectedImportFile;
    public ImportFile? SelectedImportFile 
    {
        get => _selectedImportFile;
        private set => this.RaiseAndSetIfChanged(ref _selectedImportFile, value);
    }
    
    public bool HasSelectedFile => !string.IsNullOrWhiteSpace(SelectedFileName);
    public bool HasSelectedMappingProfile => !string.IsNullOrWhiteSpace(SelectedMappingProfile);
    
    // Optional (if you want the button disabled until file picked)
    public bool CanContinue => HasSelectedFile && HasSelectedMappingProfile;

    public ReactiveCommand<Unit, Unit> PickFile { get; }
    public ReactiveCommand<Unit, Unit> ContinueToPreview { get; }

    private readonly Action<string, string> _onContinueToPreview;

    public ImportViewModel(Action<string, string> onContinueToPreview)
    {
        _onContinueToPreview = onContinueToPreview;

        SelectedMappingProfile = MappingProfiles.Count > 0
            ? MappingProfiles[0]
            : null;

        // Observable that emits true only when a file is selected
        var canContinue = this
            .WhenAnyValue(vm => vm.SelectedFileName)
            .Select(name => !string.IsNullOrWhiteSpace(name));
        
        PickFile = ReactiveCommand.Create(() => { /* handled in View */ });

        ContinueToPreview = ReactiveCommand.Create(
            execute: () =>
            {
                _onContinueToPreview(SelectedFileAbsolutePath!, SelectedMappingProfile!);
            },
            canExecute: canContinue
        );
    }

    public void SetSelectedFile(IStorageItem file)
    {
        SelectedFileName = file.Name;
        SelectedFileAbsolutePath = file.Path.AbsolutePath;
        SelectedImportFile = new ImportFile(
            FileName: file.Name,
            AbsolutePath: file.Path.AbsolutePath);
        
        this.RaisePropertyChanged(nameof(HasSelectedFile));
        this.RaisePropertyChanged(nameof(CanContinue));
    }

    public async Task LoadFileAsync(IStorageItem file)
    {
        SetSelectedFile(file);
        await Task.CompletedTask;
    }
}