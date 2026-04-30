using System.IO;
using Google.Protobuf;
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
    private readonly SnapshotImportService.SnapshotImportServiceClient _snapshotClient;
    private readonly CommitImportService.CommitImportServiceClient _commitClient;

    public GrpcVoxelCatalogService(string address)
    {
        var channel = GrpcChannel.ForAddress(address);

        _modelClient = new ModelQueryService.ModelQueryServiceClient(channel);
        _versionClient = new VersionQueryService.VersionQueryServiceClient(channel);
        _restoreClient = new ModelRestoreService.ModelRestoreServiceClient(channel);
        _snapshotClient = new SnapshotImportService.SnapshotImportServiceClient(channel);
        _commitClient = new CommitImportService.CommitImportServiceClient(channel);
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
                x.Kind,
                string.IsNullOrEmpty(x.ParentVersionId) ? null : Guid.Parse(x.ParentVersionId),
                x.Commit != null
                    ? new CommitInfo(
                        x.Commit.Message,
                        UnixTimeStampToDateTime(x.Commit.CreatedAtUtc))
                    : null))
            .ToList();
    }

    public async Task DownloadModelAsync(Guid versionId, int chunkSize, Stream destination,
        CancellationToken ct = default)
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

    public async Task<Guid> UploadSnapshotAsync(string modelName, int chunkSize, Stream source,
        CancellationToken ct = default)
    {
        using var call = _snapshotClient.UploadSnapshot();

        var buffer = new byte[64 * 1024];
        var isFirst = true;

        while (true)
        {
            var read = await source.ReadAsync(buffer, 0, buffer.Length, ct);
            if (read == 0)
                break;

            var request = new UploadSnapshotRequest
            {
                Data = ByteString.CopyFrom(buffer, 0, read)
            };

            if (isFirst)
            {
                request.ModelName = modelName;
                request.ChunkSize = chunkSize;
                isFirst = false;
            }

            await call.RequestStream.WriteAsync(request);
        }

        await call.RequestStream.CompleteAsync();

        var response = await call.ResponseAsync;
        return Guid.Parse(response.VersionId);
    }

    public async Task<Guid> UploadCommitAsync(Guid modelId, Guid parentVersionId, string commitMessage, int chunkSize,
        Stream source, CancellationToken ct = default)
    {
        using var call = _commitClient.UploadCommit();

        var buffer = new byte[64 * 1024];
        var isFirst = true;

        while (true)
        {
            var read = await source.ReadAsync(buffer, 0, buffer.Length, ct);
            if (read == 0)
                break;

            var request = new UploadCommitRequest
            {
                Data = ByteString.CopyFrom(buffer, 0, read)
            };

            if (isFirst)
            {
                request.ModelId = modelId.ToString();
                request.ParentVersionId = parentVersionId.ToString();
                request.Message = commitMessage;
                request.ChunkSize = chunkSize;
                isFirst = false;
            }

            await call.RequestStream.WriteAsync(request);
        }

        await call.RequestStream.CompleteAsync();

        var response = await call.ResponseAsync;
        return Guid.Parse(response.VersionId);
    }

    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }
}