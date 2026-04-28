using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using VoxHub.Models;
using VoxHub.Services;

namespace VoxHub.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IVoxelCatalogService _catalogService;

    public ObservableCollection<ModelListItem> Models { get; } = new();
    public ObservableCollection<VersionListItem> Versions { get; } = new();

    private ModelListItem? _selectedModel;

    public ModelListItem? SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (ReferenceEquals(_selectedModel, value))
                return;

            _selectedModel = value;
            OnPropertyChanged();
            _ = LoadVersionsAsync();
        }
    }

    private VersionListItem? _selectedVersion;

    public VersionListItem? SelectedVersion
    {
        get => _selectedVersion;
        set
        {
            if (ReferenceEquals(_selectedVersion, value))
                return;

            _selectedVersion = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel(IVoxelCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task LoadAsync()
    {
        Models.Clear();
        foreach (var model in await _catalogService.GetModelsAsync())
            Models.Add(model);

        SelectedModel = Models.Count > 0 ? Models[0] : null;
    }

    private async Task LoadVersionsAsync()
    {
        Versions.Clear();

        if (SelectedModel is null)
            return;

        var versions = await _catalogService.GetVersionsAsync(SelectedModel.Id);
        foreach (var version in versions)
            Versions.Add(version);

        SelectedVersion = Versions.Count > 0 ? Versions[0] : null;
    }

    public async Task DownloadSelectedVersionAsync(string filePath)
    {
        if (SelectedVersion is null)
            return;

        await using var fs = File.Create(filePath);

        await _catalogService.DownloadModelAsync(
            versionId: SelectedVersion.Id,
            chunkSize: 16,
            destination: fs);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}