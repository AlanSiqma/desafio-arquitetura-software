using DesafioArquiteturaSoftware.Domain.Entities;
using DesafioArquiteturaSoftware.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DesafioArquiteturaSoftware.Infrastructure.Repositories;

public sealed class IdempotencyRepository: IIdempotencyRepository
{
    private readonly AppDbContext _context;

    public IdempotencyRepository(
        AppDbContext context)
    {
        _context = context;
    }

    public async Task<IdempotencyRecord?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken)
    {
        return await _context.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Key == key,
                cancellationToken);
    }

    public async Task AddAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken)
    {
        await _context.IdempotencyRecords.AddAsync(
            record,
            cancellationToken);
    }
}