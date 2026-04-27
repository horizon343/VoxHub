using Grpc.Net.Client;
using VoxHub.Models;
using VoxHubService.Grpc;

namespace VoxHub.Services;

public sealed class GrpcVoxelCatalogService : IVoxelCatalogService
{
    private readonly ModelQueryService.ModelQueryServiceClient _client;

    public GrpcVoxelCatalogService(string address)
    {
        var channel = GrpcChannel.ForAddress(address);
        _client = new ModelQueryService.ModelQueryServiceClient(channel);
    }

    public async Task<IReadOnlyList<ModelListItem>> GetModelsAsync(CancellationToken ct = default)
    {
        var response = await _client.ListModelsAsync(new ListModelsRequest(), cancellationToken: ct);

        return response.Models
            .Select(x => new ModelListItem(Guid.Parse(x.Id), x.Name))
            .ToList();
    }

    // пока сервер не реализовал — возвращаем пусто
    public Task<IReadOnlyList<VersionListItem>> GetVersionsAsync(Guid modelId, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<VersionListItem>>(Array.Empty<VersionListItem>());
    }
}