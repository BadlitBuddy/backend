using Api.Application;
using Api.Infrastructure;
using Api.Infrastructure.Data;
using Api.Web;
using Aspire.ServiceDefaults;
using Hangfire;
using Scalar.AspNetCore;
using Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

var app = builder.Build();

await app.InitialiseDatabaseAsync();

app.UseForwardedHeaders();
app.UseCors("CorsPolicy");
app.UseRateLimiter();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthFilter()]
});

app.UseHttpsRedirection();
app.MapOpenApi();
app.MapScalarApiReference();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler(options => { });
app.Map("/", () => Results.Redirect("/scalar"));

app.MapEndpoints(typeof(Program).Assembly);

app.Run();
