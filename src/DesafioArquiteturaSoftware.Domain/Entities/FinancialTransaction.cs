using DesafioArquiteturaSoftware.Domain.Enums;

namespace DesafioArquiteturaSoftware.Domain.Entities;

public class FinancialTransaction
{
    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public string Description { get; private set; }

    public decimal Amount { get; private set; }

    public TransactionType Type { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Guid Version { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    private FinancialTransaction()
    {
            Description = string.Empty;

    }

    public FinancialTransaction(
        Guid accountId,
        string description,
        decimal amount,
        TransactionType type)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException(
                "AccountId is required.");
        }

        Validate(description, amount);
        AccountId = accountId;
        Id = Guid.NewGuid();
        Description = description;
        Amount = amount;
        Type = type;
        CreatedAt = DateTime.UtcNow;
        Version = Guid.NewGuid();
        IsDeleted = false;

    }
    public void Update(
       string description,
       decimal amount,
       TransactionType type)
    {
        if (IsDeleted)
            throw new InvalidOperationException(
                "A deleted transaction cannot be updated.");

        Validate(description, amount);

        Description = description;
        Amount = amount;
        Type = type;
        UpdatedAt = DateTime.UtcNow;
        Version = Guid.NewGuid();
    }

    public void Delete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Version = Guid.NewGuid();
    }

    private static void Validate(
     string description,
     decimal amount)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.");

        if (description.Length > 200)
            throw new ArgumentException(
                "Description cannot exceed 200 characters.");

        if (amount <= 0)
            throw new ArgumentException(
                "Amount must be greater than zero.");
    }
}