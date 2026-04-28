using System.IO;
using System.Text;
using VoxHub.Domain.Canonical;
using VoxHub.Domain.Vox;
using VoxHub.Interfaces;

namespace VoxHub.Domain.Importing;

public sealed class VoxModelImporter : IModelImporter
{
    public Task<VoxelModel> ImportAsync(Stream input, CancellationToken cancellationToken = default)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        _ = cancellationToken;

        using var reader = new BinaryReader(input, Encoding.ASCII, leaveOpen: true);

        if (ReadId(reader) != "VOX ") throw new InvalidDataException("Invalid VOX header.");
        if (reader.ReadInt32() != 150) throw new InvalidDataException("Unsupported VOX version.");

        if (ReadId(reader) != "MAIN") throw new InvalidDataException("MAIN chunk is missing.");
        var mainContentSize = reader.ReadInt32();
        var mainChildrenSize = reader.ReadInt32();
        if (mainContentSize != 0 || mainChildrenSize < 0) throw new InvalidDataException("Invalid MAIN chunk.");

        var mainChildren = reader.ReadBytes(mainChildrenSize);
        if (mainChildren.Length != mainChildrenSize) throw new EndOfStreamException();

        using var chunks = new BinaryReader(new MemoryStream(mainChildren), Encoding.ASCII);

        VoxSize? size = null;
        var voxels = new List<Voxel>();
        var palette = new List<VoxelColor>(255);

        var packSeen = false;
        var sizeSeen = false;
        var xyziSeen = false;

        while (chunks.BaseStream.Position < chunks.BaseStream.Length)
        {
            var id = ReadId(chunks);
            var contentSize = chunks.ReadInt32();
            var childSize = chunks.ReadInt32();

            if (contentSize < 0 || childSize != 0)
                throw new InvalidDataException("Nested chunks are not supported.");

            var content = chunks.ReadBytes(contentSize);
            if (content.Length != contentSize)
                throw new EndOfStreamException();

            using var contentReader = new BinaryReader(new MemoryStream(content), Encoding.ASCII);

            if (id == "PACK")
            {
                if (packSeen || sizeSeen) throw new InvalidDataException("Invalid PACK position.");
                if (contentSize != 4) throw new InvalidDataException("Invalid PACK chunk.");
                if (contentReader.ReadInt32() != 1)
                    throw new NotSupportedException("Only single-model .vox files are supported.");
                packSeen = true;
            }
            else if (id == "SIZE")
            {
                if (sizeSeen || xyziSeen) throw new InvalidDataException("Unexpected SIZE chunk.");
                if (contentSize != 12) throw new InvalidDataException("Invalid SIZE chunk.");

                size = new VoxSize(
                    contentReader.ReadInt32(),
                    contentReader.ReadInt32(),
                    contentReader.ReadInt32());

                sizeSeen = true;
            }
            else if (id == "XYZI")
            {
                if (!sizeSeen || xyziSeen) throw new InvalidDataException("Unexpected XYZI chunk.");
                if (contentSize < 4) throw new InvalidDataException("Invalid XYZI chunk.");

                var count = contentReader.ReadInt32();
                if (count < 0 || contentSize != 4L + (long)count * 4L)
                    throw new InvalidDataException("Invalid XYZI chunk.");

                for (var i = 0; i < count; i++)
                {
                    voxels.Add(new Voxel(
                        new Int3(contentReader.ReadByte(), contentReader.ReadByte(), contentReader.ReadByte()),
                        contentReader.ReadByte()));
                }

                xyziSeen = true;
            }
            else if (id == "RGBA")
            {
                if (!xyziSeen) throw new InvalidDataException("RGBA must follow model data.");
                if (contentSize != 1024) throw new InvalidDataException("Invalid RGBA chunk.");

                // 256 colors in file, first one is reserved for index 0.
                for (var i = 0; i < 256; i++)
                {
                    var r = contentReader.ReadByte();
                    var g = contentReader.ReadByte();
                    var b = contentReader.ReadByte();
                    var a = contentReader.ReadByte();

                    if (i > 0)
                        palette.Add(new VoxelColor(r, g, b, a));
                }
            }
            else
            {
                throw new InvalidDataException($"Unknown chunk '{id}'.");
            }
        }

        if (!sizeSeen || !xyziSeen)
            throw new InvalidDataException("SIZE/XYZI pair is missing.");

        return Task.FromResult(new VoxelModel
        {
            SchemaVersion = 1,
            RootChunk = new ChunkNode
            {
                Origin = new Int3(0, 0, 0),
                Size = new Int3(size!.Value.X, size.Value.Y, size.Value.Z),
                LodLevel = 0,
                Voxels = voxels,
                Children = Array.Empty<ChunkNode>()
            },
            Palette = palette
        });
    }

    private static string ReadId(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        if (bytes.Length != 4) throw new EndOfStreamException();
        return Encoding.ASCII.GetString(bytes);
    }
}