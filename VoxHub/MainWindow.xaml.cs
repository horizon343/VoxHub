using System.Windows;
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
}