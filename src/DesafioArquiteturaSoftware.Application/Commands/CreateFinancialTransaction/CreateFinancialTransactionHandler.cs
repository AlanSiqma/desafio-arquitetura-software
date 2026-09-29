using DesafioArquiteturaSoftware.Domain.Entities;
using DesafioArquiteturaSoftware.Domain.Exceptions;
using DesafioArquiteturaSoftware.Domain.Interfaces;
using DesafioArquiteturaSoftware.Infrastructure.Repositories;

namespace DesafioArquiteturaSoftware.Application.Commands
    .CreateFinancialTransaction;

public sealed class CreateFinancialTransactionHandler
{
    private readonly IFinancialTransactionRepository
        _transactionRepository;

    private readonly IIdempotencyRepository
        _idempotencyRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    public CreateFinancialTransactionHandler(
        IFinancialTransactionRepository transactionRepository,
        IIdempotencyRepository idempotencyRepository,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository =
            transactionRepository;

        _idempotencyRepository =
            idempotencyRepository;

        _unitOfWork =
            unitOfWork;
    }

    public async Task<Guid> HandleAsync(
        CreateFinancialTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var requestHash = RequestHash.Calculate(
            new
            {
                command.Description,
                command.Amount,
                command.Type
            });

        var existing =
            await _idempotencyRepository.GetByKeyAsync(
                command.IdempotencyKey,
                cancellationToken);

        if (existing is not null)
        {
            ValidateExistingRequest(
                existing,
                requestHash);

            return existing.ResourceId;
        }

        var transaction = new FinancialTransaction(
            command.Description,
            command.Amount,
            command.Type);

        var idempotencyRecord =
            new IdempotencyRecord(
                command.IdempotencyKey,
                requestHash,
                transaction.Id);

        await _unitOfWork.BeginTransactionAsync(
            cancellationToken);

        try
        {
            await _transactionRepository.AddAsync(
                transaction,
                cancellationToken);

            await _idempotencyRepository.AddAsync(
                idempotencyRecord,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await _unitOfWork.CommitAsync(
                cancellationToken);

            return transaction.Id;
        }
        catch (IdempotencyKeyAlreadyExistsException)
        {
            await _unitOfWork.RollbackAsync(
                cancellationToken);

            _unitOfWork.ClearTracking();

            var existingAfterConflict =
                await _idempotencyRepository.GetByKeyAsync(
                    command.IdempotencyKey,
                    cancellationToken);

            if (existingAfterConflict is null)
            {
                throw;
            }

            ValidateExistingRequest(
                existingAfterConflict,
                requestHash);

            return existingAfterConflict.ResourceId;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(
                cancellationToken);

            _unitOfWork.ClearTracking();

            throw;
        }
    }

    private static void ValidateExistingRequest(
        IdempotencyRecord existing,
        string requestHash)
    {
        if (existing.RequestHash != requestHash)
        {
            throw new IdempotencyRequestMismatchException();
        }
    }
}