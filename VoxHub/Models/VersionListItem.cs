namespace VoxHub.Models;

public sealed record VersionListItem(Guid Id, string Kind)
{
    public string Display => $"{Kind} | {Id}";
}