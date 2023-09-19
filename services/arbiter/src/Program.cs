using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Grid;
using Swagger.Utility;
using Grid.Arbiter.Service;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();

builder.Services.AddSingleton(_ => GridServerArbiter.Singleton);
builder.Services.AddControllers();
builder.Services.AddSwagger();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapGrpcService<ScriptManagementApi>();
app.MapGrpcService<SoapGatewayApi>();


app.Run();
