using System.Windows;
using Microsoft.Win32;
using VoxHub.Services;
using VoxHub.ViewModels;

namespace VoxHub;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(new GrpcVoxelCatalogService("http://localhost:5152"));
        DataContext = _viewModel;

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private async void Download_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        if (vm.SelectedVersion is null)
            return;

        var dialog = new SaveFileDialog()
        {
            Filter = "VOX files (*.vox)|*.vox",
            FileName = "model.vox"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await vm.DownloadSelectedVersionAsync(dialog.FileName);
            MessageBox.Show("Download completed.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Download failed: {ex.Message}");
        }
    }
}