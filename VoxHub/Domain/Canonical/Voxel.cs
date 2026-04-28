namespace VoxHub.Domain.Canonical;

public readonly record struct Voxel(
    Int3 Position,
    byte PaletteIndex
);