using DesafioArquiteturaSoftware.Application.Commands.UpdateFinancialTransaction;
using DesafioArquiteturaSoftware.Domain.Entities;
using DesafioArquiteturaSoftware.Domain.Enums;
using DesafioArquiteturaSoftware.Domain.Exceptions;
using DesafioArquiteturaSoftware.Domain.Interfaces;
using Moq;

namespace DesafioArquiteturaSoftware.Tests.Application.Commands.UpdateFinancialTransaction;

public class UpdateFinancialTransactionHandlerTests
{
    private readonly Mock<IFinancialTransactionRepository>
        _transactionRepository;

    private readonly Mock<IUnitOfWork>
        _unitOfWork;

    private readonly UpdateFinancialTransactionHandler _handler;

    public UpdateFinancialTransactionHandlerTests()
    {
        _transactionRepository =
            new Mock<IFinancialTransactionRepository>();

        _unitOfWork =
            new Mock<IUnitOfWork>();

        _handler = new UpdateFinancialTransactionHandler(
            _transactionRepository.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateTransaction_WhenTransactionExists()
    {
        var transaction = new FinancialTransaction(
            "Aluguel",
            2000,
            TransactionType.Debit);

        var originalVersion = transaction.Version;

        var command = new UpdateFinancialTransactionCommand(
            transaction.Id,
            "Aluguel atualizado",
            2200,
            TransactionType.Debit);

        _transactionRepository
            .Setup(x => x.GetByIdAsync(
                transaction.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        await _handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.Equal(
            "Aluguel atualizado",
            transaction.Description);

        Assert.Equal(2200, transaction.Amount);

        Assert.Equal(
            TransactionType.Debit,
            transaction.Type);

        Assert.NotEqual(
            originalVersion,
            transaction.Version);

        Assert.NotNull(transaction.UpdatedAt);

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

        _unitOfWork.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenTransactionDoesNotExist()
    {
        var transactionId = Guid.NewGuid();

        var command = new UpdateFinancialTransactionCommand(
            transactionId,
            "Aluguel atualizado",
            2200,
            TransactionType.Debit);

        _transactionRepository
            .Setup(x => x.GetByIdAsync(
                transactionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinancialTransaction?)null);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.HandleAsync(
                command,
                CancellationToken.None));

        Assert.Equal(
            "Financial transaction was not found.",
            exception.Message);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenAmountIsInvalid()
    {
        var transaction = new FinancialTransaction(
            "Aluguel",
            2000,
            TransactionType.Debit);

        var command = new UpdateFinancialTransactionCommand(
            transaction.Id,
            "Aluguel atualizado",
            -100,
            TransactionType.Debit);

        _transactionRepository
            .Setup(x => x.GetByIdAsync(
                transaction.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.HandleAsync(
                command,
                CancellationToken.None));

        Assert.Equal(
            "Amount must be greater than zero.",
            exception.Message);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldPropagateConcurrencyConflict()
    {
        var transaction = new FinancialTransaction(
            "Aluguel",
            2000,
            TransactionType.Debit);

        var command = new UpdateFinancialTransactionCommand(
            transaction.Id,
            "Aluguel atualizado",
            2200,
            TransactionType.Debit);

        _transactionRepository
            .Setup(x => x.GetByIdAsync(
                transaction.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new ConcurrencyConflictException());

        var exception =
            await Assert.ThrowsAsync<ConcurrencyConflictException>(
                () => _handler.HandleAsync(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "The financial transaction was modified by another operation.",
            exception.Message);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

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

    [Fact]
    public async Task HandleAsync_ShouldRollbackAndRethrow_WhenSaveChangesFails()
    {
        var transaction = new FinancialTransaction(
            "Aluguel",
            2000,
            TransactionType.Debit);

        var command = new UpdateFinancialTransactionCommand(
            transaction.Id,
            "Aluguel atualizado",
            2200,
            TransactionType.Debit);

        var exception = new InvalidOperationException(
            "Database unavailable.");

        _transactionRepository
            .Setup(x => x.GetByIdAsync(
                transaction.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var thrownException =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.HandleAsync(
                    command,
                    CancellationToken.None));

        Assert.Same(exception, thrownException);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

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