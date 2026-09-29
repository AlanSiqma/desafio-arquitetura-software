using DesafioArquiteturaSoftware.Query.Models;
using DesafioArquiteturaSoftware.Query.Repositories;

namespace DesafioArquiteturaSoftware.Query.Queries.GetAccountBalance;

public sealed class GetAccountBalanceHandler
{
    private readonly IAccountBalanceRepository _repository;

    public GetAccountBalanceHandler(
        IAccountBalanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<AccountBalance> HandleAsync(
        GetAccountBalanceQuery query,
        CancellationToken cancellationToken)
    {
        if (query.AccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "AccountId is required.");
        }

        var at = query.At ?? DateTime.UtcNow;

        return await _repository.GetBalanceAsync(
            query.AccountId,
            at,
            cancellationToken);
    }
}