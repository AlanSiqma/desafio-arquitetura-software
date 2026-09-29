using DesafioArquiteturaSoftware.Query.Data;
using DesafioArquiteturaSoftware.Query.Queries.GetAccountBalance;
using DesafioArquiteturaSoftware.Query.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DesafioArquiteturaSoftware.Query;

public static class DependencyInjection
{
    public static IServiceCollection AddQuery(
        this IServiceCollection services)
    {
        services.AddSingleton<QueryDbConnectionFactory>();

        services.AddScoped<IAccountBalanceRepository,
            AccountBalanceRepository>();

        services.AddScoped<GetAccountBalanceHandler>();

        return services;
    }
}