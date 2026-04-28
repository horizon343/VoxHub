using System.IO;
using Grpc.Core;
using Grpc.Net.Client;
using VoxHub.Models;
using VoxHubService.Grpc;

namespace VoxHub.Services;

public sealed class GrpcVoxelCatalogService : IVoxelCatalogService
{
    private readonly ModelQueryService.ModelQueryServiceClient _modelClient;
    private readonly VersionQueryService.VersionQueryServiceClient _versionClient;
    private readonly ModelRestoreService.ModelRestoreServiceClient _restoreClient;

    public GrpcVoxelCatalogService(string address)
    {
        var channel = GrpcChannel.ForAddress(address);

        _modelClient = new ModelQueryService.ModelQueryServiceClient(channel);
        _versionClient = new VersionQueryService.VersionQueryServiceClient(channel);
        _restoreClient = new ModelRestoreService.ModelRestoreServiceClient(channel);
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

    public async Task DownloadModelAsync(Guid versionId, int chunkSize, Stream destination, CancellationToken ct = default)
    {
        var call = _restoreClient.DownloadModel(new DownloadModelRequest
        {
            VersionId = versionId.ToString(),
            ChunkSize = chunkSize
        }, cancellationToken: ct);

        await foreach (var msg in call.ResponseStream.ReadAllAsync(ct))
        {
            var data = msg.Data.ToByteArray();
            await destination.WriteAsync(data, 0, data.Length, ct);
        }

        await destination.FlushAsync(ct);
    }
}