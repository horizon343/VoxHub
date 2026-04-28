namespace VoxHub.Domain.Vox;

public sealed class VoxFile
{
    public int Version { get; init; }
    public required IReadOnlyList<VoxModel> Models { get; init; }
    public required IReadOnlyList<VoxColor> Palette { get; init; }
}