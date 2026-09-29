using DesafioArquiteturaSoftware.Query;
using DesafioArquiteturaSoftware.Query.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddQuery();

var app = builder.Build();

app.MapAccountEndpoints();

app.Run();