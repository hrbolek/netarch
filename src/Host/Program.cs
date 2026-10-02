using System.Collections.Concurrent;
using System.Text.Json.Serialization;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using WebAppArchitecture;

// ===========================================================
// TODO 1. Určení jmenných prostorů a názvů tříd z template
// ===========================================================
using WebAppArchitecture.User.Resolvers;
using WebAppArchitecture.User.Services;
using WebAppArchitecture.User.Models;


var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Services
// ============================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRazorComponents();

builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<HostOperationFilter>();
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter()
    );
});


// ============================================================
// TODO 2. Dependency Injection z template
// ============================================================
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// ============================================================
// Swagger
// ============================================================

app.UseSwagger();
app.UseSwaggerUI();


app.MapGet("/", () => Results.Ok(new
{
    message = "App běží",

    auth =
        "bez autentizace / autorizace",

    storage =
        "filesystem + in memory (pro účely testování)",

}));

// ============================================================
// TODO 3. Mapování endpointů z template
// ============================================================
app.MapUserEndpoints("/architecture");

// ============================================================
// Start
// ============================================================

app.Run(
    "http://0.0.0.0:8080"
);
