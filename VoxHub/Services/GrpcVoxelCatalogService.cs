using Grpc.Net.Client;
using VoxHub.Models;
using VoxHubService.Grpc;

namespace VoxHub.Services;

public sealed class GrpcVoxelCatalogService : IVoxelCatalogService
{
    private readonly ModelQueryService.ModelQueryServiceClient _modelClient;
    private readonly VersionQueryService.VersionQueryServiceClient _versionClient;

    public GrpcVoxelCatalogService(string address)
    {
        var channel = GrpcChannel.ForAddress(address);

        _modelClient = new ModelQueryService.ModelQueryServiceClient(channel);
        _versionClient = new VersionQueryService.VersionQueryServiceClient(channel);
    }

    public async Task<IReadOnlyList<ModelListItem>> GetModelsAsync(CancellationToken ct = default)
    {
        var response = await _modelClient.ListModelsAsync(
            new ListModelsRequest(),
            cancellationToken: ct);

        return response.Models
            .Select(x => new ModelListItem(Guid.Parse(x.Id), x.Name))
            .ToList();
    }

    public async Task<IReadOnlyList<VersionListItem>> GetVersionsAsync(Guid modelId, CancellationToken ct = default)
    {
        var response = await _versionClient.ListVersionsAsync(
            new ListVersionsRequest { ModelId = modelId.ToString() },
            cancellationToken: ct);

        return response.Versions
            .Select(x => new VersionListItem(
                Guid.Parse(x.Id),
                x.Kind))
            .ToList();
    }
}