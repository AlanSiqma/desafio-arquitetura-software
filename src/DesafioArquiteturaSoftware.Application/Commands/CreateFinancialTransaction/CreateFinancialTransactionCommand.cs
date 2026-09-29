using DesafioArquiteturaSoftware.Domain.Enums;

namespace DesafioArquiteturaSoftware.Application.Commands
    .CreateFinancialTransaction;

public sealed record CreateFinancialTransactionCommand(
     Guid AccountId,
    string Description,
    decimal Amount,
    TransactionType Type,
    string IdempotencyKey);