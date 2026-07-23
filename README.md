# Cinema Booking API

ASP.NET Core 9 backend for a cinema booking platform. The API provides authentication, email verification, password recovery, user profiles and wallets, avatar and movie poster management, cinema rooms, seat types, room seat layouts, showtimes, bookings, payments (PayOS), invoices, memberships, vouchers with a pluggable rule engine, check-ins, loyalty tiers, refunds, notifications, reviews, reports, and admin tooling.

## Technology

- .NET 9 and ASP.NET Core Web API
- Entity Framework Core 9 with SQL Server
- JWT bearer authentication and role-based authorization
- PayOS payment gateway integration
- SMTP email delivery
- Cloudinary image storage
- Swagger/OpenAPI
- Rate limiting
- Health checks
- Docker

## Architecture

The solution follows a layered Clean Architecture structure:

```text
src/
|-- CinemaBooking.API             Controllers, middleware, auth, startup, contracts
|-- CinemaBooking.Application     Use cases, validation, service interfaces, DTOs
|-- CinemaBooking.Domain          Entities and domain models
|-- CinemaBooking.Infrastructure  EF Core, repositories, SMTP, Cloudinary, PayOS
`-- CinemaBooking.Shared          Shared constants and utilities
tests/
`-- CinemaBooking.API.Tests       API and application service tests
docs/                             Feature-specific architecture documentation
```

Dependencies point inward: API and Infrastructure depend on Application, while Application depends on Domain. Domain has no external dependencies.

### Key Application Domains

```text
Application/
|-- Authentication/      Login, registration, JWT, email verification
|-- Bookings/            Booking creation and management
|-- CheckIns/            Ticket validation and check-in processing
|-- Cinemas/             Cinema management
|-- Genres/              Movie genre management
|-- Invoices/            Invoice generation
|-- LoyaltyTiers/        Loyalty tier configuration and points
|-- Membership/          Customer membership management
|-- Movie/               Movie catalog management
|-- Notifications/       Push and in-app notifications
|-- Payments/            PayOS payment processing
|-- Persons/             Person/customer management
|-- Products/            FnB product management
|-- Refunds/             Refund processing
|-- Reports/             Revenue and analytics reports
|-- Reviews/             Customer reviews and ratings
|-- Rooms/               Cinema room management
|-- RoomTypes/           Room type definitions
|-- Seats/               Seat management
|-- SeatTypes/           Seat type definitions
|-- Showtimes/           Showtime scheduling
|-- ShowtimeTypes/       Showtime type definitions
|-- Tickets/             Ticket generation and management
|-- Users/               User account management
`-- Vouchers/            Voucher system with pluggable rule engine
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server or SQL Server LocalDB
- EF Core CLI (`dotnet ef`)
- SMTP credentials for transactional email
- Cloudinary credentials for avatar and poster uploads
- PayOS credentials for payment processing

Check the installed tools:

```powershell
dotnet --version
dotnet ef --version
```

## Configuration

Configuration follows the standard ASP.NET Core precedence order: environment variables, `appsettings.{Environment}.json`, then `appsettings.json`.

### Local Development (User Secrets)

For local development, use .NET user secrets instead of committing credentials:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=CGVPremiumDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true" --project src/CinemaBooking.API
dotnet user-secrets set "Jwt:SigningKey" "replace-with-a-random-key-at-least-32-characters" --project src/CinemaBooking.API
dotnet user-secrets set "Email:FromAddress" "your-email@example.com" --project src/CinemaBooking.API
dotnet user-secrets set "Email:Username" "your-email@example.com" --project src/CinemaBooking.API
dotnet user-secrets set "Email:Password" "your-smtp-password" --project src/CinemaBooking.API
dotnet user-secrets set "Cloudinary:CloudName" "your-cloud-name" --project src/CinemaBooking.API
dotnet user-secrets set "Cloudinary:ApiKey" "your-api-key" --project src/CinemaBooking.API
dotnet user-secrets set "Cloudinary:ApiSecret" "your-api-secret" --project src/CinemaBooking.API
dotnet user-secrets set "PayOS:ClientId" "your-payos-client-id" --project src/CinemaBooking.API
dotnet user-secrets set "PayOS:ApiKey" "your-payos-api-key" --project src/CinemaBooking.API
dotnet user-secrets set "PayOS:ChecksumKey" "your-payos-checksum-key" --project src/CinemaBooking.API
dotnet user-secrets set "Gemini:Providers:0:Name" "Google" --project src/CinemaBooking.API
dotnet user-secrets set "Gemini:Providers:ApiKey" "your-api-key" --project src/CinemaBooking.API
dotnet user-secrets set "Gemini:Providers:Models" "ai-models" --project src/CinemaBooking.API
```

The remaining development defaults are defined in `src/CinemaBooking.API/appsettings.Development.json`.

### Environment Variables

Production deployments can use double-underscore environment variables as shown in `.env.example`. Key variables:

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Jwt__SigningKey` | JWT signing key (min 32 characters) |
| `Jwt__Issuer` | JWT issuer |
| `Jwt__Audience` | JWT audience |
| `Jwt__AccessTokenExpirationMinutes` | Token expiration in minutes |
| `Email__Host` | SMTP host |
| `Email__Port` | SMTP port |
| `Email__FromAddress` | Sender email address |
| `Email__Username` | SMTP username |
| `Email__Password` | SMTP password |
| `Cloudinary__CloudName` | Cloudinary cloud name |
| `Cloudinary__ApiKey` | Cloudinary API key |
| `Cloudinary__ApiSecret` | Cloudinary API secret |
| `PayOS__ClientId` | PayOS client ID |
| `PayOS__ApiKey` | PayOS API key |
| `PayOS__ChecksumKey` | PayOS checksum key |
| `PayOS__ReturnUrl` | Payment success redirect URL |
| `PayOS__CancelUrl` | Payment cancel redirect URL |
| `PayOS__WebhookUrl` | PayOS webhook receiver URL |
| `Frontend__BaseUrl` | Frontend application URL |
| `Frontend__AllowedOrigins` | CORS allowed origins |
| `ASPNETCORE_ENVIRONMENT` | Environment name |
| `ASPNETCORE_URLS` | Bind URLs |

## Run Locally

Apply pending EF Core migrations before starting the API:

```powershell
dotnet ef database update --project src/CinemaBooking.Infrastructure --startup-project src/CinemaBooking.API
```

Start the API:

```powershell
dotnet run --project src/CinemaBooking.API --launch-profile http
```

Development URLs:

- API: `http://localhost:5053`
- Swagger UI: `http://localhost:5053/swagger`

The application seeds development data during startup when `RunSeeder` is set to `true`, so the database must exist, be migrated, and be reachable before starting the API.

## Docker

### Using Docker Compose

Copy and configure the environment file:

```powershell
cp .env.example .env
# Edit .env with your actual credentials
```

Start all services:

```powershell
docker compose -f docker-compose.example.yml up --build
```

This starts:

- **SQL Server** (port 1433) with health checks
- **Database init** container that runs `database.sql` if the database doesn't exist
- **API** (port 8080) with health checks

### Building the Docker Image

```powershell
docker build -t cinema-booking-api .
```

## Build

```powershell
dotnet build CinemaBooking.sln
```

## Tests

```powershell
dotnet test tests/CinemaBooking.API.Tests/CinemaBooking.API.Tests.csproj
```

The test project uses xUnit with `WebApplicationFactory` and InMemory EF Core. It covers authorization, service behavior, contract validation, and integration scenarios across bookings, check-ins, cinemas, seats, showtimes, memberships, vouchers, payments, reports, and more.

## Project Structure

```text
CinemaBooking.sln
docker-compose.example.yml       Docker Compose for local/production deployment
Dockerfile                       Multi-stage build for the API
.env.example                     Environment variable template
database.sql                     Initial database schema
database-seed-loyalty-tiers.sql  Loyalty tier seed data
src/
|-- CinemaBooking.API/
|   |-- Controllers/             API endpoints
|   |-- Contracts/               Request/response models
|   |-- Configuration/           DI and app configuration
|   |-- Serialization/           JSON serialization settings
|   |-- Services/                API-level services
|   `-- Validation/              Request validation
|-- CinemaBooking.Application/
|   |-- <Domain>/                Feature modules (Auth, Bookings, Vouchers, etc.)
|   |-- Common/                  Shared application logic
|   |-- Configuration/           Application-level DI
|   `-- Contracts/               DTOs and service interfaces
|-- CinemaBooking.Domain/
|   `-- Entities/                Domain entities
|-- CinemaBooking.Infrastructure/
|   |-- Migrations/              EF Core migrations
|   |-- Persistence/             DbContext and configurations
|   |-- Repositories/            Repository implementations
|   |-- Services/                Infrastructure service implementations
|   |-- Payments/PayOS/          PayOS payment integration
|   |-- Email/                   SMTP email service
|   |-- Storage/                 Cloudinary image storage
|   |-- BackgroundJobs/          Background job processing
|   `-- Notifications/           Notification infrastructure
`-- CinemaBooking.Shared/
    |-- Constants/               Shared constants and enums
    |-- Configuration/           Shared configuration models
    `-- Time/                    Time-related utilities
tests/
`-- CinemaBooking.API.Tests/     Test project
docs/                            Feature-specific documentation
```

## Documentation

The `docs/` folder contains detailed architecture documentation for specific features:

- `VOUCHER_SYSTEM_ARCHITECTURE.md` - Voucher system with rule engine
- `CHECKIN_MODULE.md` - Ticket check-in and validation
- `RULE_TYPES_API.md` - Voucher rule types API reference
- `FNB_VOUCHER_AUDIT.md` - FnB voucher audit trail
- `LOYALTY_NOSHOW_AUDIT.md` - Loyalty and no-show audit
- `REVIEW_REWARD_AUDIT.md` - Review reward audit
- `NOSHOW_IMPACT_REPORT.md` - No-show impact analysis
- `MOVIE_PERSON_REFACTOR.md` - Movie/person domain refactor

## Security Notes

- Never commit database passwords, SMTP credentials, JWT signing keys, Cloudinary secrets, or PayOS keys.
- Use a random JWT signing key of at least 32 characters.
- Use an SMTP app password or provider-specific credential rather than a mailbox password.
- Keep production configuration in environment variables or a dedicated secret store.
- The `.env` file is gitignored. Never rename or remove it from `.gitignore`.

## License

No license file is currently present in the repository.
