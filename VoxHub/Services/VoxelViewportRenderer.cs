using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using VoxHub.Domain.Canonical;

namespace VoxHub.Services;

public static class VoxelViewportRenderer
{
    public static Point3D GetModelCenter(VoxelModel model)
    {
        var voxels = EnumerateLeafChunks(model.RootChunk).SelectMany(x => x.Voxels).ToArray();
        if (voxels.Length == 0)
            return new Point3D(0, 0, 0);

        var minX = voxels.Min(v => v.Position.X);
        var minY = voxels.Min(v => v.Position.Y);
        var minZ = voxels.Min(v => v.Position.Z);

        var maxX = voxels.Max(v => v.Position.X) + 1;
        var maxY = voxels.Max(v => v.Position.Y) + 1;
        var maxZ = voxels.Max(v => v.Position.Z) + 1;

        return new Point3D(
            (minX + maxX) / 2.0,
            (minY + maxY) / 2.0,
            (minZ + maxZ) / 2.0);
    }
    
    public static void Render(Viewport3D viewport, VoxelModel model)
    {
        viewport.Children.Clear();

        var group = new Model3DGroup();
        group.Children.Add(new AmbientLight(Colors.White));

        foreach (var leaf in EnumerateLeafChunks(model.RootChunk))
        {
            foreach (var colorGroup in leaf.Voxels
                         .Where(v => v.PaletteIndex != 0)
                         .GroupBy(v => v.PaletteIndex))
            {
                var positions = colorGroup.Select(v => v.Position).ToArray();
                var mesh = BuildMesh(positions);
                if (mesh.Positions.Count == 0)
                    continue;

                var color = GetPaletteColor(model, colorGroup.Key);

                // main colored pass
                var mainMaterial = CreateMaterial(color);
                group.Children.Add(new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = mainMaterial,
                    BackMaterial = mainMaterial
                });
                
                // edge lines только на границах граней (внешние ребра)
                var edgeMesh = BuildExternalEdgeMesh(positions);
                if (edgeMesh.Positions.Count > 0)
                {
                    var edgeMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)));
                    group.Children.Add(new GeometryModel3D
                    {
                        Geometry = edgeMesh,
                        Material = edgeMaterial,
                        BackMaterial = edgeMaterial
                    });
                }
            }
        }

        viewport.Children.Add(new ModelVisual3D { Content = group });
    }

    private static Material CreateMaterial(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();

        var material = new MaterialGroup();
        material.Children.Add(new EmissiveMaterial(brush));
        material.Children.Add(new DiffuseMaterial(brush));
        material.Freeze();

        return material;
    }

    public static IEnumerable<ChunkNode> EnumerateLeafChunks(ChunkNode node)
    {
        if (node.Children.Count == 0)
        {
            yield return node;
            yield break;
        }

        foreach (var child in node.Children)
        foreach (var leaf in EnumerateLeafChunks(child))
            yield return leaf;
    }

    private static MeshGeometry3D BuildMesh(Int3[] voxels)
    {
        var mesh = new MeshGeometry3D();
        if (voxels.Length == 0) return mesh;

        var occupied = voxels.ToHashSet();

        foreach (var p in voxels)
        {
            if (!occupied.Contains(new Int3(p.X + 1, p.Y, p.Z))) AddFace(mesh, p, 0);
            if (!occupied.Contains(new Int3(p.X - 1, p.Y, p.Z))) AddFace(mesh, p, 1);
            if (!occupied.Contains(new Int3(p.X, p.Y + 1, p.Z))) AddFace(mesh, p, 2);
            if (!occupied.Contains(new Int3(p.X, p.Y - 1, p.Z))) AddFace(mesh, p, 3);
            if (!occupied.Contains(new Int3(p.X, p.Y, p.Z + 1))) AddFace(mesh, p, 4);
            if (!occupied.Contains(new Int3(p.X, p.Y, p.Z - 1))) AddFace(mesh, p, 5);
        }

        mesh.Freeze();
        return mesh;
    }

    private static MeshGeometry3D BuildExternalEdgeMesh(Int3[] voxels)
    {
        var mesh = new MeshGeometry3D();
        if (voxels.Length == 0) return mesh;

        var occupied = voxels.ToHashSet();
        var externalEdges = new HashSet<(Point3D, Point3D)>();

        // Собираем все ребра, которые находятся только на внешних гранях
        foreach (var p in voxels)
        {
            var x = p.X;
            var y = p.Y;
            var z = p.Z;

            // Проверяем каждую грань
            // Грань +X
            if (!occupied.Contains(new Int3(p.X + 1, p.Y, p.Z)))
            {
                AddFaceEdges(externalEdges, new[]
                {
                    new Point3D(x + 1, y, z),
                    new Point3D(x + 1, y + 1, z),
                    new Point3D(x + 1, y + 1, z + 1),
                    new Point3D(x + 1, y, z + 1)
                });
            }

            // Грань -X
            if (!occupied.Contains(new Int3(p.X - 1, p.Y, p.Z)))
            {
                AddFaceEdges(externalEdges, new[]
                {
                    new Point3D(x, y, z),
                    new Point3D(x, y, z + 1),
                    new Point3D(x, y + 1, z + 1),
                    new Point3D(x, y + 1, z)
                });
            }

            // Грань +Y
            if (!occupied.Contains(new Int3(p.X, p.Y + 1, p.Z)))
            {
                AddFaceEdges(externalEdges, new[]
                {
                    new Point3D(x, y + 1, z),
                    new Point3D(x, y + 1, z + 1),
                    new Point3D(x + 1, y + 1, z + 1),
                    new Point3D(x + 1, y + 1, z)
                });
            }

            // Грань -Y
            if (!occupied.Contains(new Int3(p.X, p.Y - 1, p.Z)))
            {
                AddFaceEdges(externalEdges, new[]
                {
                    new Point3D(x, y, z),
                    new Point3D(x + 1, y, z),
                    new Point3D(x + 1, y, z + 1),
                    new Point3D(x, y, z + 1)
                });
            }

            // Грань +Z
            if (!occupied.Contains(new Int3(p.X, p.Y, p.Z + 1)))
            {
                AddFaceEdges(externalEdges, new[]
                {
                    new Point3D(x, y, z + 1),
                    new Point3D(x + 1, y, z + 1),
                    new Point3D(x + 1, y + 1, z + 1),
                    new Point3D(x, y + 1, z + 1)
                });
            }

            // Грань -Z
            if (!occupied.Contains(new Int3(p.X, p.Y, p.Z - 1)))
            {
                AddFaceEdges(externalEdges, new[]
                {
                    new Point3D(x, y, z),
                    new Point3D(x, y + 1, z),
                    new Point3D(x + 1, y + 1, z),
                    new Point3D(x + 1, y, z)
                });
            }
        }

        // Добавляем ребра в меш как маленькие цилиндры
        foreach (var (start, end) in externalEdges)
        {
            AddEdgeLine(mesh, start, end);
        }

        mesh.Freeze();
        return mesh;
    }

    private static void AddFaceEdges(HashSet<(Point3D, Point3D)> edges, Point3D[] corners)
    {
        // 4 ребра квадратной грани
        AddEdge(edges, corners[0], corners[1]);
        AddEdge(edges, corners[1], corners[2]);
        AddEdge(edges, corners[2], corners[3]);
        AddEdge(edges, corners[3], corners[0]);
    }

    private static void AddEdge(HashSet<(Point3D, Point3D)> edges, Point3D a, Point3D b)
    {
        // Нормализуем ребро чтобы избежать дубликатов
        var edge = (a.X + a.Y + a.Z) < (b.X + b.Y + b.Z) ? (a, b) : (b, a);
        edges.Add(edge);
    }

    private static void AddEdgeLine(MeshGeometry3D mesh, Point3D start, Point3D end)
    {
        const double thickness = 0.08;
        var direction = end - start;
        
        if (direction.Length == 0)
            return;

        direction.Normalize();
        
        Vector3D up = new Vector3D(0, 1, 0);
        if (Math.Abs(Vector3D.DotProduct(direction, up)) > 0.9)
            up = new Vector3D(1, 0, 0);

        var right = Vector3D.CrossProduct(direction, up);
        right.Normalize();
        up = Vector3D.CrossProduct(right, direction);
        up.Normalize();

        right *= thickness / 2;
        up *= thickness / 2;

        var idx = mesh.Positions.Count;

        mesh.Positions.Add(start + right + up);
        mesh.Positions.Add(start - right + up);
        mesh.Positions.Add(start - right - up);
        mesh.Positions.Add(start + right - up);

        mesh.Positions.Add(end + right + up);
        mesh.Positions.Add(end - right + up);
        mesh.Positions.Add(end - right - up);
        mesh.Positions.Add(end + right - up);

        // Top
        mesh.TriangleIndices.Add(idx); mesh.TriangleIndices.Add(idx + 4); mesh.TriangleIndices.Add(idx + 1);
        mesh.TriangleIndices.Add(idx + 1); mesh.TriangleIndices.Add(idx + 4); mesh.TriangleIndices.Add(idx + 5);

        // Bottom
        mesh.TriangleIndices.Add(idx + 2); mesh.TriangleIndices.Add(idx + 6); mesh.TriangleIndices.Add(idx + 3);
        mesh.TriangleIndices.Add(idx + 3); mesh.TriangleIndices.Add(idx + 6); mesh.TriangleIndices.Add(idx + 7);

        // Front
        mesh.TriangleIndices.Add(idx); mesh.TriangleIndices.Add(idx + 3); mesh.TriangleIndices.Add(idx + 4);
        mesh.TriangleIndices.Add(idx + 4); mesh.TriangleIndices.Add(idx + 3); mesh.TriangleIndices.Add(idx + 7);

        // Back
        mesh.TriangleIndices.Add(idx + 1); mesh.TriangleIndices.Add(idx + 5); mesh.TriangleIndices.Add(idx + 2);
        mesh.TriangleIndices.Add(idx + 2); mesh.TriangleIndices.Add(idx + 5); mesh.TriangleIndices.Add(idx + 6);
    }

    private static void AddFace(MeshGeometry3D mesh, Int3 p, int face)
    {
        var x = p.X;
        var y = p.Y;
        var z = p.Z;

        Point3D a, b, c, d;

        switch (face)
        {
            case 0:
                a = new Point3D(x + 1, y, z);
                b = new Point3D(x + 1, y + 1, z);
                c = new Point3D(x + 1, y + 1, z + 1);
                d = new Point3D(x + 1, y, z + 1);
                break;
            case 1:
                a = new Point3D(x, y, z);
                b = new Point3D(x, y, z + 1);
                c = new Point3D(x, y + 1, z + 1);
                d = new Point3D(x, y + 1, z);
                break;
            case 2:
                a = new Point3D(x, y + 1, z);
                b = new Point3D(x, y + 1, z + 1);
                c = new Point3D(x + 1, y + 1, z + 1);
                d = new Point3D(x + 1, y + 1, z);
                break;
            case 3:
                a = new Point3D(x, y, z);
                b = new Point3D(x + 1, y, z);
                c = new Point3D(x + 1, y, z + 1);
                d = new Point3D(x, y, z + 1);
                break;
            case 4:
                a = new Point3D(x, y, z + 1);
                b = new Point3D(x + 1, y, z + 1);
                c = new Point3D(x + 1, y + 1, z + 1);
                d = new Point3D(x, y + 1, z + 1);
                break;
            default:
                a = new Point3D(x, y, z);
                b = new Point3D(x, y + 1, z);
                c = new Point3D(x + 1, y + 1, z);
                d = new Point3D(x + 1, y, z);
                break;
        }

        var start = mesh.Positions.Count;

        mesh.Positions.Add(a);
        mesh.Positions.Add(b);
        mesh.Positions.Add(c);
        mesh.Positions.Add(d);

        mesh.TriangleIndices.Add(start);
        mesh.TriangleIndices.Add(start + 1);
        mesh.TriangleIndices.Add(start + 2);
        mesh.TriangleIndices.Add(start);
        mesh.TriangleIndices.Add(start + 2);
        mesh.TriangleIndices.Add(start + 3);
    }

    private static Color GetPaletteColor(VoxelModel model, byte paletteIndex)
    {
        var index = paletteIndex - 1;
        if (index < 0 || index >= model.Palette.Count)
            index = 0;

        var c = model.Palette[index];
        return Color.FromArgb(c.A, c.B, c.G, c.R);
    }
    
    // Добавить в класс VoxelViewportRenderer (под existing methods)
    public static GeometryModel3D? AppendHighlightToViewport(Viewport3D viewport, Int3[] highlightVoxels, Color color, double opacity)
    {
        if (highlightVoxels == null || highlightVoxels.Length == 0)
            return null;

        // Находим первый ModelVisual3D (тот, где лежит основной group)
        var existing = viewport.Children.OfType<ModelVisual3D>().FirstOrDefault();
        if (existing?.Content is not Model3DGroup group)
            return null; // ничего не рисуем, если базового контента нет

        // Построим меш для переданных вокселей (используем тот же BuildMesh)
        var mesh = BuildMesh(highlightVoxels);
        if (mesh.Positions.Count == 0)
            return null;

        // цвет с учётом opacity
        var brush = new SolidColorBrush(Color.FromArgb((byte)(Math.Clamp(opacity, 0.0, 1.0) * 255), color.R, color.G, color.B));
        brush.Freeze();

        var mat = new MaterialGroup();
        // Чтобы подсветка была видна независимо от освещения — используем Emissive + Diffuse
        mat.Children.Add(new EmissiveMaterial(brush));
        mat.Children.Add(new DiffuseMaterial(brush));
        mat.Freeze();

        var overlay = new GeometryModel3D
        {
            Geometry = mesh,
            Material = mat,
            BackMaterial = mat
        };

        // Добавляем поверх основного содержимого
        group.Children.Add(overlay);

        return overlay;
    }

    public static void RemoveOverlayFromViewport(Viewport3D viewport, GeometryModel3D? overlay)
    {
        if (overlay == null)
            return;

        var existing = viewport.Children.OfType<ModelVisual3D>().FirstOrDefault();
        if (existing?.Content is Model3DGroup group)
        {
            if (group.Children.Contains(overlay))
                group.Children.Remove(overlay);
        }
    }
    
    // публичный обёрток чтобы получить все voxels для модели
    public static Int3[] GetAllVoxelPositions(VoxelModel model)
    {
        return EnumerateLeafChunks(model.RootChunk).SelectMany(ch => ch.Voxels.Select(v => v.Position)).ToArray();
    }

// публичный обёрток чтобы получить map position->palette
    public static Dictionary<Int3, byte> GetVoxelMap(VoxelModel model)
    {
        var dict = new Dictionary<Int3, byte>();
        foreach (var leaf in EnumerateLeafChunks(model.RootChunk))
        {
            foreach (var v in leaf.Voxels)
                dict[v.Position] = v.PaletteIndex;
        }
        return dict;
    }
}