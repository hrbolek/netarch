using WebAppArchitecture.DigitalProcesses;
using WebAppArchitecture.Hosting;

var host = ArchitectureHost.Create(args);

host.Services
    .AddCore()
    .AddDigitalProcesses();

var app = host.Build();

app
    .MapCore()
    .MapDigitalProcesses();

await app.RunAsync("http://0.0.0.0:8080");
