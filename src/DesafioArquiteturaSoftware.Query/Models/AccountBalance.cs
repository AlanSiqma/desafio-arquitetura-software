namespace DesafioArquiteturaSoftware.Query.Models;

public sealed class AccountBalance
{
    public Guid AccountId { get; init; }

    public decimal Balance { get; init; }

    public DateTime AsOf { get; init; }
}