# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/CombinationGenerator.Domain/*.csproj src/CombinationGenerator.Domain/
COPY src/CombinationGenerator.Application/*.csproj src/CombinationGenerator.Application/
COPY src/CombinationGenerator.Infrastructure/*.csproj src/CombinationGenerator.Infrastructure/
COPY src/CombinationGenerator.Api/*.csproj src/CombinationGenerator.Api/
RUN dotnet restore src/CombinationGenerator.Api/CombinationGenerator.Api.csproj

COPY src/ src/
RUN dotnet publish src/CombinationGenerator.Api/CombinationGenerator.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CombinationGenerator.Api.dll"]
