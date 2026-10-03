FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317 AS build
WORKDIR /source
COPY src/CitySurfers.Domain/CitySurfers.Domain.csproj src/CitySurfers.Domain/
COPY src/CitySurfers.Application/CitySurfers.Application.csproj src/CitySurfers.Application/
COPY src/CitySurfers.Infrastructure/CitySurfers.Infrastructure.csproj src/CitySurfers.Infrastructure/
COPY src/CitySurfers.Api/CitySurfers.Api.csproj src/CitySurfers.Api/
RUN dotnet restore src/CitySurfers.Api/CitySurfers.Api.csproj
COPY src/ src/
RUN dotnet publish src/CitySurfers.Api/CitySurfers.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:222759b391a1aaf241166672c8f99b2d4ada452e7b5319f3c6e8f265a37b5ad4 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build /app .
ENTRYPOINT ["dotnet", "CitySurfers.Api.dll"]
