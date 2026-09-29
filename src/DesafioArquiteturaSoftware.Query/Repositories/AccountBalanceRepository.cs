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
                            WHEN type = 1 THEN amount
                            WHEN type = 2 THEN -amount
                            ELSE 0
                        END
                    ),
                    0
                ) AS "Balance",
                @At AS "AsOf"
            FROM financial_transactions
            WHERE account_id = @AccountId
              AND is_deleted = false
              AND created_at <= @At;
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