using DesafioArquiteturaSoftware.Api.Endpoints;
using DesafioArquiteturaSoftware.Api.Exceptions;
using DesafioArquiteturaSoftware.Application;
using DesafioArquiteturaSoftware.Infrastructure;
using DesafioArquiteturaSoftware.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    await dbContext.Database.MigrateAsync();
}

app.MapFinancialTransactionEndpoints();

app.Run();