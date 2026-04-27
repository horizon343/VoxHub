using VoxHub.Models;

namespace VoxHub.Services;

// Временная заглушка, чтобы UI уже работал.
// Потом заменим на gRPC-сервис.
public sealed class MockVoxelCatalogService : IVoxelCatalogService
{
    private static readonly IReadOnlyList<ModelListItem> Models =
    [
        new ModelListItem(Guid.Parse("11111111-1111-1111-1111-111111111111"), "test-model"),
        new ModelListItem(Guid.Parse("22222222-2222-2222-2222-222222222222"), "test-model-2")
    ];

    public Task<IReadOnlyList<ModelListItem>> GetModelsAsync(CancellationToken ct = default)
        => Task.FromResult(Models);

    public Task<IReadOnlyList<VersionListItem>> GetVersionsAsync(Guid modelId, CancellationToken ct = default)
    {
        IReadOnlyList<VersionListItem> versions =
            modelId == Models[0].Id
                ?
                [
                    new VersionListItem(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Snapshot"),
                    new VersionListItem(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Commit")
                ]
                :
                [
                    new VersionListItem(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Snapshot")
                ];

        return Task.FromResult(versions);
    }
}