using DesafioArquiteturaSoftware.Domain.Enums;

namespace DesafioArquiteturaSoftware.Application.Commands.UpdateFinancialTransaction;

public sealed record UpdateFinancialTransactionCommand(
    Guid Id,
    string Description,
    decimal Amount,
    TransactionType Type);