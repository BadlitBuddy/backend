using Api.Application;
using Api.Infrastructure;
using Api.Web;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

builder.Services
    .AddConnectionStringsConfiguration(builder.Configuration)
    .AddHangFireStorage();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

var app = builder.Build();

// TODO: protect this
app.UseHangFireDashboard();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
    
    app.MapHealthChecks("/health");

    app.MapHealthChecks("/alive", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live")
    });
}

app.UseHttpsRedirection();
app.Run();