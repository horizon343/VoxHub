namespace VoxHub.Models;

public sealed record CommitInfo(string Message, DateTime CreatedAt);

public sealed record VersionListItem(Guid Id, string Kind, Guid? ParentVersionId = null, CommitInfo? CommitInfo = null)
{
    public string Display => $"{Kind} | {Id:N}".Substring(0, 20) + "...";
}