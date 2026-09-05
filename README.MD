# BadlitBuddy Backend

BadlitBuddy is a scalable, distributed audio transcription backend built using **.NET 10**, designed to process media files using **Whisper.net** (a C# wrapper for whisper.cpp) and third-party whisper providers (**Groq**, **Cloudflare Workers AI**). It features a decoupled architecture composed of an ASP.NET Core Web API and autonomous Whisper Worker Services orchestrated via **Hangfire** and **Redis**, with optional orchestration and telemetry via **.NET Aspire**.

![alt text](app-architecture.png)

---

## Architecture Overview

The system architecture is designed for high scalability, allowing multiple Whisper worker instances to be deployed on different machines (with varying CPU/GPU capabilities) to consume transcription jobs concurrently.

```
                  ┌──────────────────────┐
                  │       React UI       │
                  └──────────┬───────────┘
                             │ (1) Request Presigned URL
                             ▼
 ┌──────────┐     ┌──────────────────────┐
 │          │◄────┤  ASP.NET Core Web API│
 │          │ (2) └──────────┬───────────┘
 │          │     Send       │ (3) Enqueue
 │Cloudflare│     Presigned  │     Job
 │  R2 /    │     URL        ▼
 │  AWS S3  │     ┌──────────────────────┐
 │          │◄────┤  Hangfire + Redis    │
 │          │ (5) └──────────┬───────────┘
 │          │     Download   │ (4) Pull Job
 │          │     & Upload   ▼
 │          │     ┌──────────────────────┐
 │          │◄────┤Whisper Worker Service│
 └──────────┘     └──────────────────────┘
```

### Flow of Data:

1. **Upload Request**: The client requests a presigned upload URL from the Web API.
2. **Direct Upload**: The client uploads the audio/video file directly to **Cloudflare R2 / AWS S3** storage using the presigned URL.
3. **Queue Job**: The API enqueues a transcription job to **Redis** using **Hangfire**.
4. **Retrieve & Process**: An available **Whisper Worker Service** pulls the job from Hangfire, downloads the target media file from R2/S3, and runs it through the configured engine (**Whisper.net**, **Groq**, or **Cloudflare Workers AI**).
5. **Save & Notify**: The worker uploads the completed transcription file (JSON, SRT, VTT, TXT) back to R2/S3, updates status in PostgreSQL, and publishes completion events via Redis pub/sub.

---

## Project Structure

The project is structured following Clean Architecture principles:

- **Api.Web**: ASP.NET Core Web API entrypoint. Handles user authentication (JWT & Google OAuth with ASP.NET Core Identity), API endpoints (Scalar API documentation), presigned storage URLs, transcription metadata, and Hangfire dashboard.
- **Api.Application** & **Api.Domain**: Houses the API's core business logic, domain entities (transcriptions, organizations, subscriptions), interfaces, and CQRS/application workflows.
- **Api.Infrastructure**: Database persistence using **Entity Framework Core** with **PostgreSQL** (`Npgsql.EntityFrameworkCore.PostgreSQL`), JWT token services, Google auth, and interceptors for auditing/soft-delete.
- **Api.BackgroundServices**: _(Legacy)_ Background services containing earlier prototypes like `S3PollerWorker` (previously used for local bucket polling).
- **WhisperService.WorkerService**: A .NET Worker Service running a Hangfire server instance to process queued transcription jobs using the configured transcription provider.
- **WhisperService.Core** & **WhisperService.Infrastructure**: Implements transcription-specific components, including the Whisper engine wrapper, audio extraction/processing (FFMpegCore, NAudio), and external API clients (Groq, Cloudflare Workers AI).
- **Aspire.AppHost**, **Aspire.ServiceDefaults**, & **Aspire.Shared**: .NET Aspire orchestration and shared telemetry defaults for running and observing the distributed system locally.
- **ModelDownloader**: _(Legacy / Unused)_ Utility CLI previously used to download pre-trained GGML Whisper models. **Note:** ModelDownloader is no longer used since model downloading is handled directly via Docker and CI/CD pipelines.
- **Shared Modules**:
  - **Shared.Abstractions**: Common interfaces and abstract contracts (storage, jobs, repositories, services).
  - **Shared.Common**: Utility helpers and shared functionality.
  - **Shared.Contracts**: Data transfer objects (DTOs), API contracts, and application-wide constants/enums (languages, model options, transcription providers).
  - **Shared.Infrastructure**: Shared cross-cutting concerns such as AWS S3 / Cloudflare R2 client setup, Hangfire Redis storage, Dapper repository access, Redis pub/sub messaging, and job execution implementations.

---

## Tech Stack

- **Runtime & Orchestration**: .NET 10, .NET Aspire
- **API Documentation**: OpenAPI / Scalar (`Scalar.AspNetCore`)
- **Authentication**: ASP.NET Core Identity with JWT Bearer and Google OAuth
- **Queue & Orchestration**: Hangfire with Redis (`Hangfire.Redis.StackExchange`)
- **Transcription Engines & Providers**:
  - **Whisper.net** (utilizing whisper.cpp runtime with GGML models)
  - **Groq Whisper API**
  - **Cloudflare Workers AI Whisper**
- **Storage**: Cloudflare R2 / AWS S3 compatibility via `AWSSDK.S3`
- **Database**: PostgreSQL (Entity Framework Core for API data access & Dapper for worker repositories)
- **Audio Processing**: FFMpegCore, NAudio
- **Messaging**: Redis Pub/Sub (`StackExchange.Redis`)
