using System.IO;
using VoxHub.Domain.Canonical;

namespace VoxHub.Interfaces;

public interface IModelImporter
{
    Task<VoxelModel> ImportAsync(
        Stream input,
        CancellationToken cancellationToken = default
    );
}