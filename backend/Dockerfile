FROM mcr.microsoft.com/dotnet/sdk:10.0.201 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/Haggly.Domain/Haggly.Domain.csproj src/Haggly.Domain/
COPY src/Haggly.Application/Haggly.Application.csproj src/Haggly.Application/
COPY src/Haggly.Infrastructure/Haggly.Infrastructure.csproj src/Haggly.Infrastructure/
COPY src/Haggly.Api/Haggly.Api.csproj src/Haggly.Api/

RUN dotnet restore src/Haggly.Api/Haggly.Api.csproj

COPY . .

RUN dotnet publish src/Haggly.Api/Haggly.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "Haggly.Api.dll"]
