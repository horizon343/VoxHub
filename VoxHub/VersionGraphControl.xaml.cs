using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using VoxHub.Models;

namespace VoxHub;

public partial class VersionGraphControl : UserControl
{
    private const double NodeRadius = 8;
    private const double NodeSpacing = 60;
    private const double LevelSpacing = 50;

    public event Action<Guid>? VersionSelected;

    private Point _panStart;
    private Vector _panOffset = new(0, 0);
    private bool _isPanning;

    public VersionGraphControl()
    {
        InitializeComponent();
        
        GraphCanvas.MouseWheel += GraphCanvas_MouseWheel;
        GraphCanvas.MouseDown += GraphCanvas_MouseDown;
        GraphCanvas.MouseMove += GraphCanvas_MouseMove;
        GraphCanvas.MouseUp += GraphCanvas_MouseUp;
    }

    public void DrawGraph(IReadOnlyList<VersionListItem> versions, Guid? selectedVersionId)
    {
        GraphCanvas.Children.Clear();

        if (versions.Count == 0)
            return;

        var versionLevels = BuildVersionTree(versions);
        
        DrawConnections(versions, versionLevels);

        foreach (var version in versions)
        {
            var (rowIndex, columnIndex) = versionLevels[version.Id];
            
            double x = 30 + columnIndex * NodeSpacing + _panOffset.X;
            double y = 20 + rowIndex * LevelSpacing + _panOffset.Y;

            DrawNode(x, y, version.Id, selectedVersionId, version.Display);
        }
    }

    private Dictionary<Guid, (int RowIndex, int ColumnIndex)> BuildVersionTree(IReadOnlyList<VersionListItem> versions)
    {
        var levels = new Dictionary<Guid, (int, int)>();
        var versionMap = versions.ToDictionary(v => v.Id);
        
        var roots = versions.Where(v => v.ParentVersionId == null || !versionMap.ContainsKey(v.ParentVersionId.Value)).ToList();
        
        int column = 0;
        int currentRow = 0;
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();

        foreach (var root in roots)
        {
            queue.Enqueue(root.Id);
            levels[root.Id] = (currentRow, column);
            column += 2;
            visited.Add(root.Id);
        }

        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();
            var (parentRow, parentColumn) = levels[parentId];
            var children = versions.Where(v => v.ParentVersionId == parentId && !visited.Contains(v.Id)).ToList();

            int childColumn = parentColumn;
            int childRow = parentRow + 1;

            foreach (var child in children)
            {
                levels[child.Id] = (childRow, childColumn);
                childColumn++;
                queue.Enqueue(child.Id);
                visited.Add(child.Id);
            }
        }

        return levels;
    }

    private void DrawConnections(IReadOnlyList<VersionListItem> versions, Dictionary<Guid, (int RowIndex, int ColumnIndex)> levels)
    {
        var versionMap = versions.ToDictionary(v => v.Id);
        var brush = new SolidColorBrush(Color.FromArgb(120, 88, 166, 255));

        foreach (var version in versions)
        {
            if (version.ParentVersionId == null || !versionMap.ContainsKey(version.ParentVersionId.Value))
                continue;

            var (parentRow, parentColumn) = levels[version.ParentVersionId.Value];
            var (childRow, childColumn) = levels[version.Id];

            double x1 = 30 + parentColumn * NodeSpacing + _panOffset.X;
            double y1 = 20 + parentRow * LevelSpacing + _panOffset.Y;

            double x2 = 30 + childColumn * NodeSpacing + _panOffset.X;
            double y2 = 20 + childRow * LevelSpacing + _panOffset.Y;

            var pathFigure = new PathFigure { StartPoint = new Point(x1, y1) };
            
            double controlY = (y1 + y2) / 2;
            var curve = new BezierSegment
            {
                Point1 = new Point(x1, controlY),
                Point2 = new Point(x2, controlY),
                Point3 = new Point(x2, y2)
            };
            pathFigure.Segments.Add(curve);

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            var path = new Path
            {
                Data = pathGeometry,
                Stroke = brush,
                StrokeThickness = 2,
                Fill = null
            };

            GraphCanvas.Children.Add(path);
        }
    }

    private void DrawNode(double x, double y, Guid versionId, Guid? selectedVersionId, string display)
    {
        var isSelected = versionId == selectedVersionId;
        
        var circle = new Ellipse
        {
            Width = NodeRadius * 2,
            Height = NodeRadius * 2,
            Fill = new SolidColorBrush(isSelected ? Color.FromArgb(255, 31, 113, 235) : Color.FromArgb(255, 88, 166, 255)),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
            StrokeThickness = isSelected ? 3 : 1,
            Cursor = Cursors.Hand
        };

        Canvas.SetLeft(circle, x - NodeRadius);
        Canvas.SetTop(circle, y - NodeRadius);

        circle.MouseLeftButtonDown += (s, e) =>
        {
            VersionSelected?.Invoke(versionId);
            e.Handled = true;
        };

        circle.MouseEnter += (s, e) =>
        {
            var tooltip = new ToolTip 
            { 
                Content = display,
                Background = new SolidColorBrush(Color.FromArgb(255, 21, 27, 35)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 61, 68, 77)),
                BorderThickness = new Thickness(1)
            };
            circle.ToolTip = tooltip;
        };

        GraphCanvas.Children.Add(circle);
    }

    private void GraphCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
    }

    private void GraphCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.MiddleButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
        {
            _isPanning = true;
            _panStart = e.GetPosition(GraphCanvas);
            GraphCanvas.Cursor = Cursors.Hand;
            e.Handled = true;
        }
    }

    private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning)
            return;

        var currentPosition = e.GetPosition(GraphCanvas);
        var delta = currentPosition - _panStart;

        _panOffset += delta;
        _panStart = currentPosition;

        var versions = GraphCanvas.Tag as IReadOnlyList<VersionListItem>;
        if (versions != null)
        {
            RedrawGraph(versions, null);
        }
    }

    private void GraphCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
        GraphCanvas.Cursor = Cursors.Arrow;
    }

    private void RedrawGraph(IReadOnlyList<VersionListItem> versions, Guid? selectedVersionId)
    {
        GraphCanvas.Children.Clear();

        if (versions.Count == 0)
            return;

        var versionLevels = BuildVersionTree(versions);
        DrawConnections(versions, versionLevels);

        foreach (var version in versions)
        {
            var (rowIndex, columnIndex) = versionLevels[version.Id];
            double x = 30 + columnIndex * NodeSpacing + _panOffset.X;
            double y = 20 + rowIndex * LevelSpacing + _panOffset.Y;
            DrawNode(x, y, version.Id, selectedVersionId, version.Display);
        }
    }

    public void UpdateGraph(IReadOnlyList<VersionListItem> versions, Guid? selectedVersionId)
    {
        GraphCanvas.Tag = versions;
        DrawGraph(versions, selectedVersionId);
    }
}