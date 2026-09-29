namespace DesafioArquiteturaSoftware.Domain.Entities;

public class IdempotencyRecord
{
    public Guid Id { get; private set; }

    public string Key { get; private set; }

    public string RequestHash { get; private set; }

    public Guid ResourceId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private IdempotencyRecord()
    {
        Key = string.Empty;
        RequestHash = string.Empty;
    }

    public IdempotencyRecord(
        string key,
        string requestHash,
        Guid resourceId)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(
                "Idempotency key is required.");

        if (string.IsNullOrWhiteSpace(requestHash))
            throw new ArgumentException(
                "Request hash is required.");

        Id = Guid.NewGuid();
        Key = key;
        RequestHash = requestHash;
        ResourceId = resourceId;
        CreatedAt = DateTime.UtcNow;
    }
}