using DesafioArquiteturaSoftware.Domain.Interfaces;
using DesafioArquiteturaSoftware.Infrastructure.Data;
using DesafioArquiteturaSoftware.Infrastructure.Data.Repository;
using DesafioArquiteturaSoftware.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DesafioArquiteturaSoftware.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IFinancialTransactionRepository,FinancialTransactionRepository>();
        services.AddScoped<IIdempotencyRepository,IdempotencyRepository>();

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        return services;
    }
}