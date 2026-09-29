using DesafioArquiteturaSoftware.Domain.Enums;

namespace DesafioArquiteturaSoftware.Api.Contracts.Transactions;

public sealed record UpdateFinancialTransactionRequest(
    string Description,
    decimal Amount,
    TransactionType Type);