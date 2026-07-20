FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY CinemaBooking.sln ./
COPY src/CinemaBooking.API/CinemaBooking.API.csproj src/CinemaBooking.API/
COPY src/CinemaBooking.Application/CinemaBooking.Application.csproj src/CinemaBooking.Application/
COPY src/CinemaBooking.Domain/CinemaBooking.Domain.csproj src/CinemaBooking.Domain/
COPY src/CinemaBooking.Infrastructure/CinemaBooking.Infrastructure.csproj src/CinemaBooking.Infrastructure/
COPY src/CinemaBooking.Shared/CinemaBooking.Shared.csproj src/CinemaBooking.Shared/

RUN dotnet restore src/CinemaBooking.API/CinemaBooking.API.csproj

COPY src/ src/

RUN dotnet publish src/CinemaBooking.API/CinemaBooking.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "CinemaBooking.API.dll"]
