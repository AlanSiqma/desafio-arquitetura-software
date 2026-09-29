namespace DesafioArquiteturaSoftware.Query.Queries.GetAccountBalance;

public sealed record GetAccountBalanceQuery(
    Guid AccountId,
    DateTime? At);