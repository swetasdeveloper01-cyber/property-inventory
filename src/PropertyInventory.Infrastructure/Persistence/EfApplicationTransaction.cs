using Microsoft.EntityFrameworkCore.Storage;
using PropertyInventory.Application.Common.Interfaces;

namespace PropertyInventory.Infrastructure.Persistence;

internal sealed class EfApplicationTransaction : IApplicationTransaction
{
    private readonly IDbContextTransaction _transaction;

    public EfApplicationTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        _transaction.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
