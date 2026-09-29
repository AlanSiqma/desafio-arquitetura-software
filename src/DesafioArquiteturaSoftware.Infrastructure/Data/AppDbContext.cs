using DesafioArquiteturaSoftware.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DesafioArquiteturaSoftware.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<FinancialTransaction> FinancialTransactions => 
        Set<FinancialTransaction>();

    public DbSet<IdempotencyRecord> IdempotencyRecords =>
    Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }


}