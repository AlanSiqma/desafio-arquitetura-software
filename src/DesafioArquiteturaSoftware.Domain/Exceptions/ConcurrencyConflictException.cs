namespace DesafioArquiteturaSoftware.Domain.Exceptions;

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("The financial transaction was modified by another operation.")
    {
    }
}