namespace PropertyInventory.Application.Common.Interfaces;

/// <summary>
/// Minimal transaction handle for multi-step business operations (for example, ownership transfer).
/// </summary>
public interface IApplicationTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
