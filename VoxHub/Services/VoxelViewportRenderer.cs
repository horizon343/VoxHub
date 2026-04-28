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
            var chunkCenter = new Point3D(
                leaf.Origin.X + leaf.Size.X / 2.0,
                leaf.Origin.Y + leaf.Size.Y / 2.0,
                leaf.Origin.Z + leaf.Size.Z / 2.0);

            foreach (var colorGroup in leaf.Voxels
                         .Where(v => v.PaletteIndex != 0)
                         .GroupBy(v => v.PaletteIndex))
            {
                var positions = colorGroup.Select(v => v.Position).ToArray();
                var mesh = BuildMesh(positions);
                if (mesh.Positions.Count == 0)
                    continue;

                var color = GetPaletteColor(model, colorGroup.Key);

// основной материал
                var mainMaterial = new DiffuseMaterial(new SolidColorBrush(color));

// outline материал (полупрозрачный!)
                var outlineMaterial = new DiffuseMaterial(
                    new SolidColorBrush(Color.FromArgb(120, 20, 20, 20))); // не чёрный, а мягкий тёмный

// 1. СНАЧАЛА основной меш
                group.Children.Add(new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = mainMaterial,
                    BackMaterial = mainMaterial
                });

// 2. ПОТОМ outline (слегка увеличенный)
                group.Children.Add(new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = outlineMaterial,
                    BackMaterial = outlineMaterial,
                    Transform = new ScaleTransform3D(1.05, 1.05, 1.05,
                        chunkCenter.X, chunkCenter.Y, chunkCenter.Z)
                });
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

    private static IEnumerable<ChunkNode> EnumerateLeafChunks(ChunkNode node)
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
        return Color.FromArgb(c.A, c.R, c.G, c.B);
    }
}