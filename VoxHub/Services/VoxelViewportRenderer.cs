using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using VoxHub.Domain.Canonical;

namespace VoxHub.Services;

public static class VoxelViewportRenderer
{
    public static void Render(Viewport3D viewport, VoxelModel model)
    {
        viewport.Children.Clear();

        var group = new Model3DGroup
        {
            Children =
            {
                new AmbientLight(Colors.White)
            }
        };

        foreach (var leaf in EnumerateLeafChunks(model.RootChunk))
        {
            foreach (var colorGroup in leaf.Voxels
                         .Where(v => v.PaletteIndex != 0)
                         .GroupBy(v => v.PaletteIndex))
            {
                var mesh = BuildMesh(colorGroup.Select(v => v.Position).ToArray());
                if (mesh.Positions.Count == 0)
                    continue;

                var color = GetPaletteColor(model, colorGroup.Key);
                var material = new DiffuseMaterial(new SolidColorBrush(color));
                material.Freeze();

                group.Children.Add(new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material
                });
            }
        }

        group.Freeze();
        viewport.Children.Add(new ModelVisual3D { Content = group });
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

    // One mesh per color group, with only visible faces.
    private static MeshGeometry3D BuildMesh(Int3[] voxels)
    {
        var mesh = new MeshGeometry3D();

        if (voxels.Length == 0)
            return mesh;

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
            case 0: // +X
                a = new Point3D(x + 1, y, z);
                b = new Point3D(x + 1, y + 1, z);
                c = new Point3D(x + 1, y + 1, z + 1);
                d = new Point3D(x + 1, y, z + 1);
                break;
            case 1: // -X
                a = new Point3D(x, y, z);
                b = new Point3D(x, y, z + 1);
                c = new Point3D(x, y + 1, z + 1);
                d = new Point3D(x, y + 1, z);
                break;
            case 2: // +Y
                a = new Point3D(x, y + 1, z);
                b = new Point3D(x, y + 1, z + 1);
                c = new Point3D(x + 1, y + 1, z + 1);
                d = new Point3D(x + 1, y + 1, z);
                break;
            case 3: // -Y
                a = new Point3D(x, y, z);
                b = new Point3D(x + 1, y, z);
                c = new Point3D(x + 1, y, z + 1);
                d = new Point3D(x, y, z + 1);
                break;
            case 4: // +Z
                a = new Point3D(x, y, z + 1);
                b = new Point3D(x + 1, y, z + 1);
                c = new Point3D(x + 1, y + 1, z + 1);
                d = new Point3D(x, y + 1, z + 1);
                break;
            default: // -Z
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
            return Colors.White;

        var c = model.Palette[index];
        return Color.FromArgb(c.A, c.R, c.G, c.B);
    }
}