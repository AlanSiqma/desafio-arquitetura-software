using DesafioArquiteturaSoftware.Domain.Interfaces;

namespace DesafioArquiteturaSoftware.Application.Commands.DeleteFinancialTransaction;

public sealed class DeleteFinancialTransactionHandler
{
    private readonly IFinancialTransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFinancialTransactionHandler(
        IFinancialTransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        DeleteFinancialTransactionCommand command,
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

        transaction.Delete();

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