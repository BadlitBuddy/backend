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
builder.AddWebConfiguration(builder.Configuration);
builder.AddWebServices();

builder.Services
    .AddConnectionStringsConfiguration(builder.Configuration)
    .AddS3Configuration(builder.Configuration)
    .AddS3Services()
    .AddHangFireStorage()
    .AddRedisSubscriberService();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

var app = builder.Build();

app.UseHangFireDashboard();
app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.MapOpenApi();
app.MapScalarApiReference();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();

    app.MapHealthChecks("/health");

    app.MapHealthChecks("/alive", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live")
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints(typeof(Program).Assembly);

app.UseHttpsRedirection();
app.Run();
