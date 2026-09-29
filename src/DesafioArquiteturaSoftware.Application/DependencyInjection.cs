using DesafioArquiteturaSoftware.Application.Commands
    .CreateFinancialTransaction;
using DesafioArquiteturaSoftware.Application.Commands.DeleteFinancialTransaction;
using DesafioArquiteturaSoftware.Application.Commands.UpdateFinancialTransaction;
using Microsoft.Extensions.DependencyInjection;

namespace DesafioArquiteturaSoftware.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<CreateFinancialTransactionHandler>();
        services.AddScoped<UpdateFinancialTransactionHandler>();
        services.AddScoped<DeleteFinancialTransactionHandler>();

        return services;
    }
}