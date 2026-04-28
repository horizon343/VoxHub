namespace VoxHub.Domain.Vox;

public readonly record struct VoxVoxel(
    byte X,
    byte Y,
    byte Z,
    byte ColorIndex
);