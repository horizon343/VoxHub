using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using VoxHub.Domain.Canonical;
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
    private readonly Dictionary<Guid, VoxelModel> _modelCache = new();

    // Primary viewport state
    private bool _rotating;
    private Point _lastMouse;
    private double _yaw = 45;
    private double _pitch = 20;
    private double _distance = 120;
    private Point3D _target = new(0, 0, 0);

    // Secondary viewport state
    private bool _secondaryRotating;
    private Point _secondaryLastMouse;
    private double _secondaryYaw = 45;
    private double _secondaryPitch = 20;
    private double _secondaryDistance = 120;
    private Point3D _secondaryTarget = new(0, 0, 0);

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(new GrpcVoxelCatalogService("http://localhost:5152"));
        DataContext = _viewModel;

        Loaded += MainWindow_Loaded;
        SizeChanged += (s, e) => VersionGraph.DrawGraph(_viewModel.Versions, _viewModel.SelectedVersion?.Id);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    
        // Подписываемся на события графа версий
        VersionGraph.VersionSelected += OnVersionSelected;
    
        // Подписываемся на изменения в ViewModel
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Versions) || 
                e.PropertyName == nameof(_viewModel.SelectedVersion))
            {
                VersionGraph.UpdateGraph(_viewModel.Versions, _viewModel.SelectedVersion?.Id);
            }
        };
    
        // Рисуем граф после загрузки
        VersionGraph.UpdateGraph(_viewModel.Versions, _viewModel.SelectedVersion?.Id);
        
        VersionGraph.VersionRightClicked += (versionId) =>
        {
            OnVersionRightClicked(versionId);
        };
    }

    private void OnVersionSelected(Guid versionId)
    {
        // Ищем версию в списке и выбираем её
        var version = _viewModel.Versions.FirstOrDefault(v => v.Id == versionId);
        if (version != null)
        {
            _viewModel.SelectedVersion = version;
            
            // Если модель в кэше, отрисовываем её
            if (_modelCache.ContainsKey(versionId))
            {
                RenderCachedModel(versionId);
            }
        }
    }

    private void RenderCachedModel(Guid versionId)
    {
        if (_modelCache.TryGetValue(versionId, out var model))
        {
            VoxelViewportRenderer.Render(Viewport, model);
            
            _target = VoxelViewportRenderer.GetModelCenter(model);
            UpdateCamera();
        }
    }

    private void OnVersionRightClicked(Guid versionId)
    {
        if (_modelCache.TryGetValue(versionId, out var model))
        {
            RenderSecondaryModel(model);
        }
    }

    private void RenderSecondaryModel(VoxelModel model)
    {
        VoxelViewportRenderer.Render(SecondaryViewport, model);
        
        _secondaryTarget = VoxelViewportRenderer.GetModelCenter(model);
        _secondaryYaw = 45;
        _secondaryPitch = 20;
        _secondaryDistance = 120;
        UpdateSecondaryCamera();

        SecondaryViewportBorder.Visibility = Visibility.Visible;
    }

    private async void Download_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.SelectedVersion is null)
        {
            MessageBox.Show("Please select a version first");
            return;
        }

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

            // Кэшируем модель
            _modelCache[vm.SelectedVersion.Id] = model;

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

    private async void UploadSnapshot_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "VOX files (*.vox)|*.vox"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            if (DataContext is not MainViewModel vm)
                return;

            var modelName = ModelNameBox.Text;
            var chunkSize = 16;

            await vm.UploadSnapshotAsync(modelName, chunkSize, dialog.FileName);
            MessageBox.Show("Snapshot uploaded successfully!");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }

    private async void CreateCommit_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedVersion is null)
        {
            MessageBox.Show("Please select a version first");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "VOX files (*.vox)|*.vox"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            if (DataContext is not MainViewModel vm)
                return;

            var commitMessage = CommitMessageBox.Text;
            var chunkSize = 16;

            UploadStatusText.Text = "Uploading commit...";
            await vm.UploadCommitAsync(chunkSize, dialog.FileName, commitMessage);
            
            UploadStatusText.Text = "✓ Commit uploaded successfully!";
            VersionGraph.UpdateGraph(vm.Versions, vm.SelectedVersion?.Id);
            MessageBox.Show("Commit created successfully!");
        }
        catch (Exception ex)
        {
            UploadStatusText.Text = $"✗ Error: {ex.Message}";
            MessageBox.Show($"Error: {ex.Message}");
        }
    }

    private async void DownloadFromGraph_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedVersion is null)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "VOX files (*.vox)|*.vox",
            FileName = $"model_{_viewModel.SelectedVersion.Id:N}.vox"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await _viewModel.DownloadSelectedVersionAsync(dialog.FileName);

            await using var fs = File.OpenRead(dialog.FileName);
            var importer = new VoxModelImporter();
            var model = await importer.ImportAsync(fs);

            // Кэшируем модель
            _modelCache[_viewModel.SelectedVersion.Id] = model;

            VoxelViewportRenderer.Render(Viewport, model);
            
            _target = VoxelViewportRenderer.GetModelCenter(model);
            UpdateCamera();

            MessageBox.Show("Version downloaded and rendered.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }

    // PRIMARY VIEWPORT MOUSE HANDLERS
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

    // SECONDARY VIEWPORT MOUSE HANDLERS
    private void UpdateSecondaryCamera()
    {
        var yawRad = _secondaryYaw * Math.PI / 180.0;
        var pitchRad = _secondaryPitch * Math.PI / 180.0;

        var x = _secondaryDistance * Math.Cos(pitchRad) * Math.Sin(yawRad);
        var y = _secondaryDistance * Math.Sin(pitchRad);
        var z = _secondaryDistance * Math.Cos(pitchRad) * Math.Cos(yawRad);

        var position = new Point3D(_secondaryTarget.X + x, _secondaryTarget.Y + y, _secondaryTarget.Z + z);

        SecondaryCamera.Position = position;
        SecondaryCamera.LookDirection = new Vector3D(_secondaryTarget.X - position.X, _secondaryTarget.Y - position.Y, _secondaryTarget.Z - position.Z);
        SecondaryCamera.UpDirection = new Vector3D(0, 1, 0);
    }

    private void SecondaryViewport_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        _secondaryRotating = true;
        _secondaryLastMouse = e.GetPosition(this);
        Mouse.Capture(SecondaryViewport);
    }

    private void SecondaryViewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_secondaryRotating)
            return;

        var current = e.GetPosition(this);
        var dx = current.X - _secondaryLastMouse.X;
        var dy = current.Y - _secondaryLastMouse.Y;

        _secondaryYaw -= dx * 0.5;
        _secondaryPitch += dy * 0.5;
        _secondaryPitch = Math.Clamp(_secondaryPitch, -89, 89);

        _secondaryLastMouse = current;
        UpdateSecondaryCamera();
    }

    private void SecondaryViewport_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _secondaryRotating = false;
        Mouse.Capture(null);
    }

    private void SecondaryViewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _secondaryDistance *= e.Delta > 0 ? 0.9 : 1.1;
        _secondaryDistance = Math.Clamp(_secondaryDistance, 20, 1000);
        UpdateSecondaryCamera();
    }

    // SYNC BUTTONS
    private void SyncSecondaryToFirst_Click(object sender, RoutedEventArgs e)
    {
        _secondaryYaw = _yaw;
        _secondaryPitch = _pitch;
        _secondaryDistance = _distance;
        _secondaryTarget = _target;
        UpdateSecondaryCamera();
    }

    private void SyncFirstToSecondary_Click(object sender, RoutedEventArgs e)
    {
        _yaw = _secondaryYaw;
        _pitch = _secondaryPitch;
        _distance = _secondaryDistance;
        _target = _secondaryTarget;
        UpdateCamera();
    }

    private void CloseSecondaryViewport_Click(object sender, RoutedEventArgs e)
    {
        SecondaryViewportBorder.Visibility = Visibility.Collapsed;
    }
}