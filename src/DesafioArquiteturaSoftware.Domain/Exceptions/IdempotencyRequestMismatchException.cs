namespace DesafioArquiteturaSoftware.Domain.Exceptions;

public sealed class IdempotencyRequestMismatchException
    : Exception
{
    public IdempotencyRequestMismatchException()
        : base(
            "The idempotency key was already used with a different request.")
    {
    }
}