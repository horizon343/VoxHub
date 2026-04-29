namespace VoxHub.Models;

public sealed record VersionListItem(Guid Id, string Kind, Guid? ParentVersionId = null)
{
    public string Display => $"{Kind} | {Id:N}".Substring(0, 20) + "...";
}