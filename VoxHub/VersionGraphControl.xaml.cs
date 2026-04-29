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
    private const double NodeSpacing = 50;
    private const double LevelSpacing = 40;

    public event Action<Guid>? VersionSelected;

    public VersionGraphControl()
    {
        InitializeComponent();
    }

    public void DrawGraph(IReadOnlyList<VersionListItem> versions, Guid? selectedVersionId)
    {
        GraphCanvas.Children.Clear();

        if (versions.Count == 0)
            return;

        // Группируем версии по уровням (строим дерево)
        var versionLevels = BuildVersionTree(versions);
        
        // Рисуем линии между версиями
        DrawConnections(versions, versionLevels);

        // Рисуем узлы
        double totalWidth = GraphCanvas.ActualWidth;
        double totalHeight = GraphCanvas.ActualHeight;
        double startX = 20;
        double startY = totalHeight / 2;

        foreach (var version in versions)
        {
            var level = versionLevels[version.Id];
            double x = startX + level.ColumnIndex * NodeSpacing;
            double y = startY + level.RowIndex * LevelSpacing - (versions.Count * LevelSpacing / 2);

            DrawNode(x, y, version.Id, selectedVersionId, version.Display);
        }
    }

    private Dictionary<Guid, (int RowIndex, int ColumnIndex)> BuildVersionTree(IReadOnlyList<VersionListItem> versions)
    {
        var levels = new Dictionary<Guid, (int, int)>();
        int column = 0;
        int row = 0;

        // Простой алгоритм: расставляем слева направо
        foreach (var version in versions)
        {
            levels[version.Id] = (row, column);
            column++;
            if (column > 5) // Переносим на новую строку если больше 5
            {
                column = 0;
                row++;
            }
        }

        return levels;
    }

    private void DrawConnections(IReadOnlyList<VersionListItem> versions, Dictionary<Guid, (int RowIndex, int ColumnIndex)> levels)
    {
        var brush = new SolidColorBrush(Color.FromArgb(100, 61, 68, 77)); // полупрозрачный цвет
        
        for (int i = 0; i < versions.Count - 1; i++)
        {
            var current = levels[versions[i].Id];
            var next = levels[versions[i + 1].Id];

            double x1 = 20 + current.ColumnIndex * NodeSpacing;
            double y1 = (GraphCanvas.ActualHeight / 2) + current.RowIndex * LevelSpacing - (versions.Count * LevelSpacing / 2);

            double x2 = 20 + next.ColumnIndex * NodeSpacing;
            double y2 = (GraphCanvas.ActualHeight / 2) + next.RowIndex * LevelSpacing - (versions.Count * LevelSpacing / 2);

            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = brush,
                StrokeThickness = 2
            };

            GraphCanvas.Children.Add(line);
        }
    }

    private void DrawNode(double x, double y, Guid versionId, Guid? selectedVersionId, string display)
    {
        var isSelected = versionId == selectedVersionId;
        
        // Кружок
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

        // Клик по версии
        circle.MouseLeftButtonDown += (s, e) =>
        {
            VersionSelected?.Invoke(versionId);
            e.Handled = true;
        };

        // Тултип
        circle.MouseEnter += (s, e) =>
        {
            var tooltip = new ToolTip { Content = display };
            circle.ToolTip = tooltip;
        };

        GraphCanvas.Children.Add(circle);
    }
}