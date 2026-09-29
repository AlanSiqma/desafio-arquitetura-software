using DesafioArquiteturaSoftware.Domain.Entities;

namespace DesafioArquiteturaSoftware.Domain.Interfaces;

public interface IFinancialTransactionRepository
{
    Task AddAsync(
        FinancialTransaction transaction,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);


    Task<FinancialTransaction?> GetByIdAsync(
      Guid id,
      CancellationToken cancellationToken);

}