using DesafioArquiteturaSoftware.Api.Contracts.Transactions;
using DesafioArquiteturaSoftware.Application.Commands.CreateFinancialTransaction;
using DesafioArquiteturaSoftware.Application.Commands.UpdateFinancialTransaction;
using DesafioArquiteturaSoftware.Application.Commands.DeleteFinancialTransaction;

namespace DesafioArquiteturaSoftware.Api.Endpoints;

public static class FinancialTransactionEndpoints
{
    public static IEndpointRouteBuilder
        MapFinancialTransactionEndpoints(
            this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/transactions",
            async (
                CreateFinancialTransactionRequest request,
                HttpRequest httpRequest,
                CreateFinancialTransactionHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!httpRequest.Headers.TryGetValue(
                        "Idempotency-Key",
                        out var idempotencyKey))
                {
                    return Results.BadRequest(new
                    {
                        error = "Idempotency-Key header is required."
                    });
                }

                var key = idempotencyKey.ToString().Trim();

                if (string.IsNullOrWhiteSpace(key))
                {
                    return Results.BadRequest(new
                    {
                        error = "Idempotency-Key cannot be empty."
                    });
                }

                if (key.Length > 100)
                {
                    return Results.BadRequest(new
                    {
                        error = "Idempotency-Key cannot exceed 100 characters."
                    });
                }

                var command = new CreateFinancialTransactionCommand(
                    request.Description,
                    request.Amount,
                    request.Type,
                    key);

                var id = await handler.HandleAsync(
                    command,
                    cancellationToken);

                return Results.Created(
                    $"/api/transactions/{id}",
                    new { id });
            });

        app.MapPut(
            "/api/transactions/{id:guid}",
            async (
                Guid id,
                UpdateFinancialTransactionRequest request,
                UpdateFinancialTransactionHandler handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateFinancialTransactionCommand(
                    id,
                    request.Description,
                    request.Amount,
                    request.Type);

                await handler.HandleAsync(
                    command,
                    cancellationToken);

                return Results.NoContent();
            });

        app.MapDelete(
            "/api/transactions/{id:guid}",
            async (
                Guid id,
                DeleteFinancialTransactionHandler handler,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteFinancialTransactionCommand(id);

                await handler.HandleAsync(
                    command,
                    cancellationToken);

                return Results.NoContent();
            });

        return app;
    }

}