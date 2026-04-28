using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using VoxHub.Domain.Importing;
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
        if (DataContext is not MainViewModel vm || vm.SelectedVersion is null)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "VOX files (*.vox)|*.vox",
            FileName = "model.vox"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await vm.DownloadSelectedVersionAsync(dialog.FileName);

            // сразу открываем и рендерим
            await using var fs = File.OpenRead(dialog.FileName);
            var importer = new VoxModelImporter();
            var model = await importer.ImportAsync(fs);

            VoxelViewportRenderer.Render(Viewport, model);
            
            _target = VoxelViewportRenderer.GetModelCenter(model);
            UpdateCamera();

            MessageBox.Show("Loaded and rendered.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }
    
    private bool _rotating;
    private Point _lastMouse;
    private double _yaw = 45;
    private double _pitch = 20;
    private double _distance = 120;
    private Point3D _target = new(0, 0, 0);

    private void UpdateCamera()
    {
        var yawRad = _yaw * Math.PI / 180.0;
        var pitchRad = _pitch * Math.PI / 180.0;

        var x = _distance * Math.Cos(pitchRad) * Math.Sin(yawRad);
        var y = _distance * Math.Sin(pitchRad);
        var z = _distance * Math.Cos(pitchRad) * Math.Cos(yawRad);

        var position = new Point3D(_target.X + x, _target.Y + y, _target.Z + z);

        Camera.Position = position;
        Camera.LookDirection = new Vector3D(_target.X - position.X, _target.Y - position.Y, _target.Z - position.Z);
        Camera.UpDirection = new Vector3D(0, 1, 0);
    }

    private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        _rotating = true;
        _lastMouse = e.GetPosition(this);
        Mouse.Capture(Viewport);
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_rotating)
            return;

        var current = e.GetPosition(this);
        var dx = current.X - _lastMouse.X;
        var dy = current.Y - _lastMouse.Y;

        _yaw -= dx * 0.5;
        _pitch += dy * 0.5;
        _pitch = Math.Clamp(_pitch, -89, 89);

        _lastMouse = current;
        UpdateCamera();
    }

    private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _rotating = false;
        Mouse.Capture(null);
    }

    private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _distance *= e.Delta > 0 ? 0.9 : 1.1;
        _distance = Math.Clamp(_distance, 20, 1000);
        UpdateCamera();
    }
}