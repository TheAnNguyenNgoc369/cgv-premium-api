# Repository Map

Use this file to locate code quickly.

---

## API Layer

Responsibilities:

* Controllers
* Middleware
* Authentication configuration
* Request/Response models
* Dependency Injection bootstrap

Typical search terms:

Controller
Program
Middleware
Authorize

---

## Application Layer

Responsibilities:

* Use Cases
* Services
* DTOs
* Validators
* Interfaces

Typical search terms:

Dto
Validator
UseCase
Service
Repository

---

## Domain Layer

Responsibilities:

* Entities
* Value Objects
* Enums
* Business Rules

Typical search terms:

Entity
ValueObject
Aggregate

---

## Infrastructure Layer

Responsibilities:

* EF Core
* DbContext
* Repositories
* External Services
* Authentication Providers

Typical search terms:

DbContext
Repository
Migration
Seed

---

# Common Search Keywords

Authentication

* Login
* Register
* Jwt
* Token
* RefreshToken
* Authorize
* CurrentUser

User Management

* User
* Profile
* GetProfile
* UpdateProfile
* ChangePassword

Database

* DbContext
* Migration
* Seed
* Seeder
* DataInitializer

Validation

* Validator
* FluentValidation

Testing

* WebApplicationFactory
* IntegrationTest
* UnitTest

---

# Implementation Strategy

When implementing a feature:

1. Find similar feature.
2. Copy pattern.
3. Modify minimally.
4. Add validation.
5. Add tests.

---

# Testing Strategy

Happy path.

Failure path.

Authorization path.

Validation path.

Only add tests relevant to the task.

---

# Common Task Breakdown

New Endpoint

* DTO
* Validator
* Service
* Controller
* Test

Bug Fix

* Reproduce
* Fix
* Verify

Database Change

* Entity
* Configuration
* Migration
* Validation

---

# Stop Condition

When requested work is complete:

Stop.

Do not continue exploring the repository.

Do not perform unrelated improvements.
