using System.Collections.Concurrent;
using System.Text.Json.Serialization;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using WebAppArchitecture;
using WebAppArchitecture.UI;
using WebAppArchitecture.Resolves;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Services
// ============================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRazorComponents();

builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<CreateSignDocumentOperationFilter>();
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter()
    );
});

// builder.Services.AddSingleton<PdfSigningService>();

// builder.Services.AddScoped<
//     IPdfDocumentService,
//     PdfDocumentService<IPdfDocumentStore>>();

// builder.Services.AddSingleton<
//     IPdfDocumentStore,
//     InMemoryPdfDocumentStore>();

var app = builder.Build();

// ============================================================
// Swagger
// ============================================================

app.UseSwagger();
app.UseSwaggerUI();

// ============================================================