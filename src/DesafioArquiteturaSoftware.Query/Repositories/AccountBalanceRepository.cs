using Dapper;
using DesafioArquiteturaSoftware.Query.Data;
using DesafioArquiteturaSoftware.Query.Models;

namespace DesafioArquiteturaSoftware.Query.Repositories;

public sealed class AccountBalanceRepository
    : IAccountBalanceRepository
{
    private readonly QueryDbConnectionFactory _connectionFactory;

    public AccountBalanceRepository(
        QueryDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AccountBalance> GetBalanceAsync(
        Guid accountId,
        DateTime at,
        CancellationToken cancellationToken)
    {
        const string sql = """
                        SELECT
                            @AccountId AS "AccountId",
                            COALESCE(
                                SUM(
                                    CASE
                                        WHEN "Type" = 1 THEN "Amount"
                                        WHEN "Type" = 2 THEN -"Amount"
                                        ELSE 0
                                    END
                                ),
                                0
                            ) AS "Balance",
                            @At AS "AsOf"
                        FROM financial_transactions
                        WHERE "AccountId" = @AccountId
                          AND "IsDeleted" = false
                          AND "CreatedAt" <= @At;
                        """;

        using var connection =
            _connectionFactory.CreateConnection();

        return await connection.QuerySingleAsync<AccountBalance>(
            new CommandDefinition(
                sql,
                new
                {
                    AccountId = accountId,
                    At = at
                },
                cancellationToken: cancellationToken));
    }
}