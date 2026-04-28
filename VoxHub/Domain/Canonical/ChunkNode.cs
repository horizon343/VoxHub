namespace VoxHub.Domain.Canonical;

public sealed class ChunkNode
{
    public required Int3 Origin { get; init; }
    public required Int3 Size { get; init; }
    public required int LodLevel { get; init; }
    public IReadOnlyList<ChunkNode> Children { get; init; } = Array.Empty<ChunkNode>();
    public IReadOnlyList<Voxel> Voxels { get; init; } = Array.Empty<Voxel>();
}