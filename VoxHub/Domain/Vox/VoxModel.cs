namespace VoxHub.Domain.Vox;

public sealed class VoxModel
{
    public required VoxSize Size { get; init; }
    public required IReadOnlyList<VoxVoxel> Voxels { get; init; }
}