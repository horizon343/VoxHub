using System.IO;
using VoxHub.Models;

namespace VoxHub.Services;

public interface IVoxelCatalogService
{
    Task<IReadOnlyList<ModelListItem>> GetModelsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<VersionListItem>> GetVersionsAsync(Guid modelId, CancellationToken ct = default);
    Task DownloadModelAsync(Guid versionId, int chunkSize, Stream destination, CancellationToken ct = default);
}