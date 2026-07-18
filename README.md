# Cinema Booking API

ASP.NET Core 9 backend for a cinema booking platform. The API provides authentication, email verification, password recovery, user profiles and wallets, avatar and movie poster management, cinema rooms, seat types, room seat layouts, showtimes, bookings, payments, invoices, and memberships.

## Technology

- .NET 9 and ASP.NET Core Web API
- Entity Framework Core 9 with SQL Server
- JWT bearer authentication and role-based authorization
- SMTP email delivery
- Cloudinary image storage
- Swagger/OpenAPI
- Docker

## Architecture

The solution follows a layered Clean Architecture structure:

```text
src/
|-- CinemaBooking.API             Controllers, request contracts, auth, startup
|-- CinemaBooking.Application     Use cases, validation, service interfaces
|-- CinemaBooking.Domain          Entities and domain models
|-- CinemaBooking.Infrastructure  EF Core, repositories, SMTP, Cloudinary
`-- CinemaBooking.Shared          Shared constants
tests/
`-- CinemaBooking.API.Tests       API and application service tests
```

Dependencies point inward: API and Infrastructure depend on Application, while Application depends on Domain.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server or SQL Server LocalDB
- EF Core CLI (`dotnet ef`)
- SMTP credentials for transactional email
- Cloudinary credentials for avatar and poster uploads

Check the installed tools:

```powershell
dotnet --version
dotnet ef --version
```

## Configuration

Configuration follows the standard ASP.NET Core precedence order: environment variables, `appsettings.{Environment}.json`, then `appsettings.json`.

For local development, use .NET user secrets instead of committing credentials:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=CVPremiumDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true" --project src/CinemaBooking.API
dotnet user-secrets set "Jwt:SigningKey" "replace-with-a-random-key-at-least-32-characters" --project src/CinemaBooking.API
dotnet user-secrets set "Email:FromAddress" "your-email@example.com" --project src/CinemaBooking.API
dotnet user-secrets set "Email:Username" "your-email@example.com" --project src/CinemaBooking.API
dotnet user-secrets set "Email:Password" "your-smtp-password" --project src/CinemaBooking.API
dotnet user-secrets set "Cloudinary:CloudName" "your-cloud-name" --project src/CinemaBooking.API
dotnet user-secrets set "Cloudinary:ApiKey" "your-api-key" --project src/CinemaBooking.API
dotnet user-secrets set "Cloudinary:ApiSecret" "your-api-secret" --project src/CinemaBooking.API
```

The remaining development defaults are defined in `src/CinemaBooking.API/appsettings.Development.json`. Production deployments can use the equivalent double-underscore environment variables shown in `.env.example`, such as `Jwt__SigningKey` and `Email__Host`.

## Run Locally

Apply pending EF Core migrations before starting the API:

```powershell
dotnet ef database update --project src/CinemaBooking.Infrastructure --startup-project src/CinemaBooking.API
```

```powershell
dotnet run --project src/CinemaBooking.API --launch-profile http
```

Development URLs:

- API: `http://localhost:5053`
- Swagger UI: `http://localhost:5053/swagger`

The application seeds its configured development data during startup, so the database must exist, be migrated, and be reachable before starting the API.

## Build

```powershell
dotnet build CinemaBooking.sln
dotnet test tests/CinemaBooking.API.Tests/CinemaBooking.API.Tests.csproj
```

The test project uses xUnit and currently covers selected API authorization and seat management service behavior.

## Security Notes

- Never commit database passwords, SMTP credentials, JWT signing keys, or Cloudinary secrets.
- Use a random JWT signing key of at least 32 characters.
- Use an SMTP app password or provider-specific credential rather than a mailbox password.
- Keep production configuration in environment variables or a dedicated secret store.
