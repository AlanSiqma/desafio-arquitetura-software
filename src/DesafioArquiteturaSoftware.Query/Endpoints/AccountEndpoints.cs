using DesafioArquiteturaSoftware.Query.Contracts.Accounts;
using DesafioArquiteturaSoftware.Query.Queries.GetAccountBalance;

namespace DesafioArquiteturaSoftware.Query.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/accounts/{accountId:guid}/balance",
            async (
                Guid accountId,
                DateTime? at,
                GetAccountBalanceHandler handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetAccountBalanceQuery(
                    accountId,
                    at);

                var balance = await handler.HandleAsync(
                    query,
                    cancellationToken);

                var response = new GetAccountBalanceResponse(
                    balance.AccountId,
                    balance.Balance,
                    balance.AsOf);

                return Results.Ok(response);
            });

        return app;
    }
}