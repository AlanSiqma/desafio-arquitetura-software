namespace DesafioArquiteturaSoftware.Query.Contracts.Accounts;

public sealed record GetAccountBalanceResponse(
    Guid AccountId,
    decimal Balance,
    DateTime AsOf);