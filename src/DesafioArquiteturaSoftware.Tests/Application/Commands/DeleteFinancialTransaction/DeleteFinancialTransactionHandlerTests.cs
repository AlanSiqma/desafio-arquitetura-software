using DesafioArquiteturaSoftware.Application.Commands.DeleteFinancialTransaction;
using DesafioArquiteturaSoftware.Domain.Entities;
using DesafioArquiteturaSoftware.Domain.Enums;
using DesafioArquiteturaSoftware.Domain.Interfaces;
using Moq;

namespace DesafioArquiteturaSoftware.Tests.Application.Commands.DeleteFinancialTransaction;

public class DeleteFinancialTransactionHandlerTests
{
    private readonly Mock<IFinancialTransactionRepository>
        _transactionRepository;

    private readonly Mock<IUnitOfWork>
        _unitOfWork;

    private readonly DeleteFinancialTransactionHandler _handler;

    public DeleteFinancialTransactionHandlerTests()
    {
        _transactionRepository =
            new Mock<IFinancialTransactionRepository>();

        _unitOfWork =
            new Mock<IUnitOfWork>();

        _handler = new DeleteFinancialTransactionHandler(
            _transactionRepository.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteTransaction_WhenTransactionExists()
    {
        var transaction = new FinancialTransaction(
            "Aluguel",
            2000,
            TransactionType.Debit);

        var originalVersion = transaction.Version;

        var command = new DeleteFinancialTransactionCommand(
            transaction.Id);

        _transactionRepository
            .Setup(x => x.GetByIdAsync(
                transaction.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        await _handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.True(transaction.IsDeleted);
        Assert.NotNull(transaction.DeletedAt);
        Assert.NotNull(transaction.UpdatedAt);
        Assert.NotEqual(
            originalVersion,
            transaction.Version);

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

        var command = new DeleteFinancialTransactionCommand(
            transactionId);

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

        var command = new DeleteFinancialTransactionCommand(
            transaction.Id);

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