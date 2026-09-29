using DesafioArquiteturaSoftware.Application;
using DesafioArquiteturaSoftware.Application.Commands.CreateFinancialTransaction;
using DesafioArquiteturaSoftware.Domain.Entities;
using DesafioArquiteturaSoftware.Domain.Enums;
using DesafioArquiteturaSoftware.Domain.Exceptions;
using DesafioArquiteturaSoftware.Domain.Interfaces;
using DesafioArquiteturaSoftware.Infrastructure.Repositories;
using Moq;

namespace DesafioArquiteturaSoftware.Tests.Application.Commands.CreateFinancialTransaction;

public class CreateFinancialTransactionHandlerTests
{
    private readonly Mock<IFinancialTransactionRepository>
        _transactionRepository;

    private readonly Mock<IIdempotencyRepository>
        _idempotencyRepository;

    private readonly Mock<IUnitOfWork>
        _unitOfWork;

    private readonly CreateFinancialTransactionHandler _handler;

    public CreateFinancialTransactionHandlerTests()
    {
        _transactionRepository = new Mock<IFinancialTransactionRepository>();
        _idempotencyRepository = new Mock<IIdempotencyRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _handler = new CreateFinancialTransactionHandler(
            _transactionRepository.Object,
            _idempotencyRepository.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateTransaction_WhenIdempotencyKeyDoesNotExist()
    {
        var command = new CreateFinancialTransactionCommand(
            "Aluguel",
            2000,
            TransactionType.Debit,
            "teste-001");

        _idempotencyRepository
            .Setup(x => x.GetByKeyAsync(
                command.IdempotencyKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyRecord?)null);

        Guid result = await _handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);

        _transactionRepository.Verify(
            x => x.AddAsync(
                It.Is<FinancialTransaction>(transaction =>
                    transaction.Id == result &&
                    transaction.Description == command.Description &&
                    transaction.Amount == command.Amount &&
                    transaction.Type == command.Type),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _idempotencyRepository.Verify(
            x => x.AddAsync(
                It.Is<IdempotencyRecord>(record =>
                    record.Key == command.IdempotencyKey &&
                    record.ResourceId == result),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnExistingResourceId_WhenSameKeyAndSameRequest()
    {
        var command = new CreateFinancialTransactionCommand(
            "Aluguel",
            2000,
            TransactionType.Debit,
            "teste-002");

        var existingResourceId = Guid.NewGuid();

        var requestHash = RequestHash.Calculate(
            new
            {
                command.Description,
                command.Amount,
                command.Type
            });

        var existingRecord = new IdempotencyRecord(
            command.IdempotencyKey,
            requestHash,
            existingResourceId);

        _idempotencyRepository
            .Setup(x => x.GetByKeyAsync(
                command.IdempotencyKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRecord);

        var result = await _handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.Equal(existingResourceId, result);

        _transactionRepository.Verify(
            x => x.AddAsync(
                It.IsAny<FinancialTransaction>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _idempotencyRepository.Verify(
            x => x.AddAsync(
                It.IsAny<IdempotencyRecord>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenSameKeyIsUsedWithDifferentRequest()
    {
        var command = new CreateFinancialTransactionCommand(
            "Aluguel",
            2000,
            TransactionType.Debit,
            "teste-003");

        var existingRecord = new IdempotencyRecord(
            command.IdempotencyKey,
            "hash-diferente",
            Guid.NewGuid());

        _idempotencyRepository
            .Setup(x => x.GetByKeyAsync(
                command.IdempotencyKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRecord);

        var exception = await Assert.ThrowsAsync<
            IdempotencyRequestMismatchException>(() =>
            _handler.HandleAsync(
                command,
                CancellationToken.None));

        Assert.Equal(
            "The idempotency key was already used with a different request.",
            exception.Message);

        _transactionRepository.Verify(
            x => x.AddAsync(
                It.IsAny<FinancialTransaction>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnExistingResourceId_WhenUniqueKeyConflictOccurs()
    {
        var command = new CreateFinancialTransactionCommand(
            "Aluguel",
            2000,
            TransactionType.Debit,
            "teste-004");

        var existingResourceId = Guid.NewGuid();

        var requestHash = RequestHash.Calculate(
            new
            {
                command.Description,
                command.Amount,
                command.Type
            });

        var existingRecord = new IdempotencyRecord(
            command.IdempotencyKey,
            requestHash,
            existingResourceId);

        _idempotencyRepository
            .SetupSequence(x => x.GetByKeyAsync(
                command.IdempotencyKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyRecord?)null)
            .ReturnsAsync(existingRecord);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new IdempotencyKeyAlreadyExistsException());

        var result = await _handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.Equal(existingResourceId, result);

        _unitOfWork.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.ClearTracking(),
            Times.Once);

        _idempotencyRepository.Verify(
            x => x.GetByKeyAsync(
                command.IdempotencyKey,
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task HandleAsync_ShouldRollbackAndRethrow_WhenUnexpectedErrorOccurs()
    {
        var command = new CreateFinancialTransactionCommand(
            "Aluguel",
            2000,
            TransactionType.Debit,
            "teste-005");

        _idempotencyRepository
            .Setup(x => x.GetByKeyAsync(
                command.IdempotencyKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyRecord?)null);

        var exception = new InvalidOperationException(
            "Database unavailable.");

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var thrownException = await Assert.ThrowsAsync<
            InvalidOperationException>(() =>
            _handler.HandleAsync(
                command,
                CancellationToken.None));

        Assert.Same(exception, thrownException);

        _unitOfWork.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.ClearTracking(),
            Times.Once);

        _unitOfWork.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}