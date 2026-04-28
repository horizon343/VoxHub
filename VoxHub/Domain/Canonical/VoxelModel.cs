namespace VoxHub.Domain.Canonical;

public sealed class VoxelModel
{
    public ushort SchemaVersion { get; init; } = 1;
    public required ChunkNode RootChunk { get; init; }

    // Palette indices are 1..255, so palette[0] corresponds to color index 1.
    public required IReadOnlyList<VoxelColor> Palette { get; init; }
}