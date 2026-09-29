using DesafioArquiteturaSoftware.Query.Models;

namespace DesafioArquiteturaSoftware.Query.Repositories;

public interface IAccountBalanceRepository
{
    Task<AccountBalance> GetBalanceAsync(
        Guid accountId,
        DateTime at,
        CancellationToken cancellationToken);
}