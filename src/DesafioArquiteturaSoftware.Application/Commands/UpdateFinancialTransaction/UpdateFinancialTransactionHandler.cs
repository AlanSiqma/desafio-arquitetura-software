using DesafioArquiteturaSoftware.Domain.Exceptions;
using DesafioArquiteturaSoftware.Domain.Interfaces;

namespace DesafioArquiteturaSoftware.Application.Commands.UpdateFinancialTransaction;

public sealed class UpdateFinancialTransactionHandler
{
    private readonly IFinancialTransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFinancialTransactionHandler(
        IFinancialTransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        UpdateFinancialTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(
            command.Id,
            cancellationToken);

        if (transaction is null)
        {
            throw new KeyNotFoundException(
                "Financial transaction was not found.");
        }

        transaction.Update(
            command.Description,
            command.Amount,
            command.Type);

        await _unitOfWork.BeginTransactionAsync(
            cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await _unitOfWork.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(
                cancellationToken);

            _unitOfWork.ClearTracking();

            throw;
        }
    }
}