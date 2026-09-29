using DesafioArquiteturaSoftware.Domain.Entities;
using DesafioArquiteturaSoftware.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DesafioArquiteturaSoftware.Infrastructure.Data.Repository;
public class FinancialTransactionRepository
    : IFinancialTransactionRepository
{
    private readonly AppDbContext _context;

    public FinancialTransactionRepository(
        AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        FinancialTransaction transaction,
        CancellationToken cancellationToken)
    {
        await _context.FinancialTransactions.AddAsync(
            transaction,
            cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FinancialTransaction?> GetByIdAsync(
       Guid id,
       CancellationToken cancellationToken)
    {
        return await _context.FinancialTransactions
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

}