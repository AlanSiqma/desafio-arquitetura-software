using DesafioArquiteturaSoftware.Domain.Entities;

namespace DesafioArquiteturaSoftware.Infrastructure.Repositories;    
public interface IIdempotencyRepository
{

    public Task<IdempotencyRecord?> GetByKeyAsync(string key,CancellationToken cancellationToken);

    public Task AddAsync(IdempotencyRecord record,CancellationToken cancellationToken);
}