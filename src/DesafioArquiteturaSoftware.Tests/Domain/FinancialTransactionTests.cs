using DesafioArquiteturaSoftware.Domain.Entities;
using DesafioArquiteturaSoftware.Domain.Enums;

namespace DesafioArquiteturaSoftware.Tests.Domain;

public class FinancialTransactionTests
{
    [Fact]
    public void Constructor_ShouldCreateTransaction_WhenDataIsValid()
    {
        var transaction = new FinancialTransaction(
            "Aluguel",
            2000,
            TransactionType.Debit);

        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal("Aluguel", transaction.Description);
        Assert.Equal(2000, transaction.Amount);
        Assert.Equal(TransactionType.Debit, transaction.Type);
        Assert.NotEqual(Guid.Empty, transaction.Version);
        Assert.False(transaction.IsDeleted);
        Assert.Null(transaction.DeletedAt);
        Assert.Null(transaction.UpdatedAt);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenDescriptionIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new FinancialTransaction(
                "",
                100,
                TransactionType.Credit));

        Assert.Equal(
            "Description is required.",
            exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenDescriptionExceeds200Characters()
    {
        var description = new string('A', 201);

        var exception = Assert.Throws<ArgumentException>(() =>
            new FinancialTransaction(
                description,
                100,
                TransactionType.Credit));

        Assert.Equal(
            "Description cannot exceed 200 characters.",
            exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenAmountIsZero()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new FinancialTransaction(
                "Teste",
                0,
                TransactionType.Credit));

        Assert.Equal(
            "Amount must be greater than zero.",
            exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenAmountIsNegative()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new FinancialTransaction(
                "Teste",
                -100,
                TransactionType.Credit));

        Assert.Equal(
            "Amount must be greater than zero.",
            exception.Message);
    }

    [Fact]
    public void Update_ShouldChangeTransactionData()
    {
        var transaction = new FinancialTransaction(
            "Aluguel",
            2000,
            TransactionType.Debit);

        var originalVersion = transaction.Version;

        transaction.Update(
            "Aluguel atualizado",
            2200,
            TransactionType.Debit);

        Assert.Equal(
            "Aluguel atualizado",
            transaction.Description);

        Assert.Equal(2200, transaction.Amount);
        Assert.Equal(TransactionType.Debit, transaction.Type);
        Assert.NotEqual(originalVersion, transaction.Version);
        Assert.NotNull(transaction.UpdatedAt);
    }

    [Fact]
    public void Update_ShouldThrow_WhenTransactionIsDeleted()
    {
        var transaction = new FinancialTransaction(
            "Aluguel",
            2000,
            TransactionType.Debit);

        transaction.Delete();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            transaction.Update(
                "Novo aluguel",
                2500,
                TransactionType.Debit));

        Assert.Equal(
            "A deleted transaction cannot be updated.",
            exception.Message);
    }

    [Fact]
    public void Delete_ShouldMarkTransactionAsDeleted()
    {
        var transaction = new FinancialTransaction(
            "Aluguel",
            2000,
            TransactionType.Debit);

        var originalVersion = transaction.Version;

        transaction.Delete();

        Assert.True(transaction.IsDeleted);
        Assert.NotNull(transaction.DeletedAt);
        Assert.NotNull(transaction.UpdatedAt);
        Assert.NotEqual(originalVersion, transaction.Version);
    }

    [Fact]
    public void Delete_ShouldBeIdempotent()
    {
        var transaction = new FinancialTransaction(
            "Aluguel",
            2000,
            TransactionType.Debit);

        transaction.Delete();

        var deletedAt = transaction.DeletedAt;
        var version = transaction.Version;

        transaction.Delete();

        Assert.True(transaction.IsDeleted);
        Assert.Equal(deletedAt, transaction.DeletedAt);
        Assert.Equal(version, transaction.Version);
    }
}