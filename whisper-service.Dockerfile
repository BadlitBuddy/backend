FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Install dependencies first (Whisper.net fails due the base image not having these)
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    libstdc++6 \
    libgomp1 \
    libatomic1 \
    && rm -rf /var/lib/apt/lists/*

# Download Whisper model
RUN mkdir -p WhisperService.Infrastructure/Models && \
    curl -L "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3-turbo-q5_0.bin?download=true" \
    -o WhisperService.Infrastructure/Models/ggml-large-v3-turbo-q5_0.bin

# Restore Project
COPY TranscriptionApp.sln .

COPY WhisperService.Core/*.csproj WhisperService.Core/
COPY WhisperService.Infrastructure/*.csproj WhisperService.Infrastructure/
COPY WhisperService.WorkerService/*.csproj WhisperService.WorkerService/

COPY Api.Domain/*.csproj Api.Domain/
COPY Shared.Common/*.csproj Shared.Common/
COPY Shared.Abstractions/*.csproj Shared.Abstractions/
COPY Shared.Contracts/*.csproj Shared.Contracts/
COPY Shared.Infrastructure/*.csproj Shared.Infrastructure/

COPY Aspire.AppHost/*.csproj Aspire.AppHost/
COPY Aspire.ServiceDefaults/*.csproj Aspire.ServiceDefaults/
COPY Aspire.Shared/*.csproj Aspire.Shared/

RUN dotnet restore WhisperService.WorkerService/WhisperService.WorkerService.csproj

# Copy source code
COPY WhisperService.Core/. WhisperService.Core/
COPY WhisperService.Infrastructure/. WhisperService.Infrastructure/
COPY WhisperService.WorkerService/. WhisperService.WorkerService/

COPY Api.Domain/. Api.Domain/
COPY Shared.Common/. Shared.Common/
COPY Shared.Abstractions/. Shared.Abstractions/
COPY Shared.Contracts/. Shared.Contracts/
COPY Shared.Infrastructure/. Shared.Infrastructure/

COPY Aspire.AppHost/. Aspire.AppHost/
COPY Aspire.ServiceDefaults/. Aspire.ServiceDefaults/
COPY Aspire.Shared/. Aspire.Shared/

# Publish 
WORKDIR /source/WhisperService.WorkerService
RUN dotnet publish -c Release -o /app --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install runtime dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    libstdc++6 \
    libgomp1 \
    libatomic1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

EXPOSE 8080

ENV PORT=8080
ENV ASPNETCORE_HTTP_PORTS=$PORT

ENTRYPOINT ["dotnet", "WhisperService.WorkerService.dll"]

