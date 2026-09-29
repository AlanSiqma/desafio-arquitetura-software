using DesafioArquiteturaSoftware.Domain.Enums;

namespace DesafioArquiteturaSoftware.Api.Contracts.Transactions;

public sealed record CreateFinancialTransactionRequest(
    string Description,
    decimal Amount,
    TransactionType Type);