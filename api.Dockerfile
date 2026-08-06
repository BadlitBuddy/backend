FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore Project
COPY TranscriptionApp.sln .

COPY Api.Application/*.csproj Api.Application/
COPY Api.BackgroundServices/*.csproj Api.BackgroundServices/
COPY Api.Domain/*.csproj Api.Domain/
COPY Api.Infrastructure/*.csproj Api.Infrastructure/
COPY Api.Web/*.csproj Api.Web/

COPY Shared.Common/*.csproj Shared.Common/
COPY Shared.Abstractions/*.csproj Shared.Abstractions/
COPY Shared.Contracts/*.csproj Shared.Contracts/
COPY Shared.Infrastructure/*.csproj Shared.Infrastructure/

RUN dotnet restore Api.Web/Api.Web.csproj

# Copy source code
COPY Api.Application/. Api.Application/
COPY Api.BackgroundServices/. Api.BackgroundServices/
COPY Api.Domain/. Api.Domain/
COPY Api.Infrastructure/. Api.Infrastructure/
COPY Api.Web/. Api.Web/

COPY Shared.Common/. Shared.Common/
COPY Shared.Abstractions/. Shared.Abstractions/
COPY Shared.Contracts/. Shared.Contracts/
COPY Shared.Infrastructure/. Shared.Infrastructure/

# Publish Api.Web
WORKDIR /source/Api.Web
RUN dotnet publish -c Release -o /app --no-restore

# Use Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/* 
    
COPY --from=build /app .

EXPOSE 8080

ENV PORT=8080
ENV ASPNETCORE_HTTP_PORTS=$PORT

ENTRYPOINT ["dotnet", "Api.Web.dll"]
