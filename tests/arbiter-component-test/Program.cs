using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Swagger.Utility;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwagger();

var app = builder.Build();

app.MapControllers();
app.UseSwagger();

app.Run();
