using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using ReactiveUI;
using ExpenseTracker.Services.Contracts;
using ExpenseTracker.UI.ViewModels;

namespace ExpenseTracker.UI.Features.Import.ImportView;

public sealed class ImportViewModel : ViewModelBase
{
    private readonly IImportService _importService;
    private readonly Action<string, string> _onContinueToPreview;

    // Dropdown items (NOT strings anymore)
    public ObservableCollection<ImportProfileOptionVm> MappingProfiles { get; } = new();

    private ImportProfileOptionVm? _selectedMappingProfile;
    public ImportProfileOptionVm? SelectedMappingProfile
    {
        get => _selectedMappingProfile;
        set => this.RaiseAndSetIfChanged(ref _selectedMappingProfile, value);
    }

    private string? _selectedFileName;
    public string? SelectedFileName
    {
        get => _selectedFileName;
        private set => this.RaiseAndSetIfChanged(ref _selectedFileName, value);
    }

    private string? _selectedFileAbsolutePath;
    public string? SelectedFileAbsolutePath
    {
        get => _selectedFileAbsolutePath;
        private set => this.RaiseAndSetIfChanged(ref _selectedFileAbsolutePath, value);
    }

    // Helpful UI flags
    public bool HasSelectedFile => !string.IsNullOrWhiteSpace(SelectedFileAbsolutePath);
    public bool HasSelectedMappingProfile => SelectedMappingProfile is not null && !string.IsNullOrWhiteSpace(SelectedMappingProfile.ProfileKey);

    public ReactiveCommand<Unit, Unit> PickFile { get; }
    public ReactiveCommand<Unit, Unit> ContinueToPreview { get; }

    // Call this from the View's WhenActivated (recommended)
    public ReactiveCommand<Unit, Unit> LoadProfiles { get; }

    private string? _profilesLoadError;
    public string? ProfilesLoadError
    {
        get => _profilesLoadError;
        private set => this.RaiseAndSetIfChanged(ref _profilesLoadError, value);
    }

    public ImportViewModel(
        IImportService importService,
        Action<string, string> onContinueToPreview)
    {
        _importService = importService;
        _onContinueToPreview = onContinueToPreview;

        PickFile = ReactiveCommand.Create(() => { /* handled in View */ });

        // Load profiles async
        LoadProfiles = ReactiveCommand.CreateFromTask(async ct =>
        {
            ProfilesLoadError = null;
            await LoadProfilesAsync(ct);
        });

        // Enable Continue only when BOTH are selected
        var canContinue = this.WhenAnyValue(
                vm => vm.SelectedFileAbsolutePath,
                vm => vm.SelectedMappingProfile,
                (path, profile) =>
                    !string.IsNullOrWhiteSpace(path) &&
                    profile is not null &&
                    !string.IsNullOrWhiteSpace(profile.ProfileKey))
            .DistinctUntilChanged();

        ContinueToPreview = ReactiveCommand.Create(
            execute: () =>
            {
                // pass (csvPath, profileKey) to next VM
                _onContinueToPreview(SelectedFileAbsolutePath!, SelectedMappingProfile!.ProfileKey);
            },
            canExecute: canContinue
        );

        // optional: surface errors (won't crash UI)
        LoadProfiles.ThrownExceptions.Subscribe(ex =>
        {
            ProfilesLoadError = ex.Message;
        });
    }

    private async Task LoadProfilesAsync(CancellationToken ct)
    {
        MappingProfiles.Clear();

        // Fetch from service
        var profiles = await _importService.GetAllProfilesAsync(ct);

        // Map to UI options
        var options = profiles
            .Select(p => new ImportProfileOptionVm(
                ProfileKey: p.ProfileKey,         // assumes IImportProfile has Key
                DisplayName: p.ProfileName        // assumes IImportProfile has Name
            ))
            .OrderBy(o => o.DisplayName)
            .ToList();

        foreach (var opt in options)
            MappingProfiles.Add(opt);

        // Pick a default selection
        SelectedMappingProfile = MappingProfiles.FirstOrDefault();
    }

    public void SetSelectedFile(IStorageItem file)
    {
        SelectedFileName = file.Name;
        SelectedFileAbsolutePath = file.Path.LocalPath;
        
        this.RaisePropertyChanged(nameof(HasSelectedFile));
    }

    public async Task LoadFileAsync(IStorageItem file)
    {
        SetSelectedFile(file);
        await Task.CompletedTask;
    }

    public sealed record ImportProfileOptionVm(string ProfileKey, string DisplayName);
}
