namespace DesafioArquiteturaSoftware.Domain.Exceptions;

public sealed class IdempotencyKeyAlreadyExistsException: Exception
{
    public IdempotencyKeyAlreadyExistsException()
        : base("The idempotency key already exists.")
    {
    }
}