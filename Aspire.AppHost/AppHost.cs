using Aspire.AppHost;
using Aspire.Shared;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddConnectionString("Redis");
var postgres = builder.AddConnectionString("Postgres");

var api = builder
    .AddProject<Projects.Api_Web>(Services.Api)
    .WithReference(redis)
    .WithReference(postgres)
    .WithExternalHttpEndpoints()
    .WithAspNetCoreEnvironment()
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar API Reference";
        url.Url = "/scalar";
    });

builder
    .AddProject<Projects.WhisperService_WorkerService>(Services.WhisperServiceDefault)
    .WithReference(redis)
    .WithReference(postgres)
    .WithEnvironment("HealthCheck__EnableHealthCheck", "false");

builder
    .AddProject<Projects.WhisperService_WorkerService>(Services.WhisperServicePublic)
    .WithReference(redis)
    .WithReference(postgres)
    .WithEnvironment("HealthCheck__EnableHealthCheck", "false")
    .WithEnvironment("HangfireServer__WorkerCount", "1")
    .WithEnvironment("HangfireServer__Queues__0", "whisper-tiny-en");

builder.Build().Run();
