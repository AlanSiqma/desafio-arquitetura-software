namespace DesafioArquiteturaSoftware.Domain.Exceptions;

public sealed class IdempotencyConflictException
    : Exception
{
    public IdempotencyConflictException()
        : base("The idempotency key already exists.")
    {
    }
}