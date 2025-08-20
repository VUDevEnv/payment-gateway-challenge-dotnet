# Instructions for candidates

This is the .NET version of the Payment Gateway challenge. If you haven't already read this [README.md](https://github.com/cko-recruitment/) on the details of this exercise, please do so now. 

## Template structure
```
src/
    PaymentGateway.Api - a skeleton ASP.NET Core Web API
test/
    PaymentGateway.Api.Tests - an empty xUnit test project
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln
```

# Payment Gateway Web API

This project is a **Payment Gateway API** built using **Clean Architecture** principles. It is designed to handle financial transactions while incorporating key resiliency patterns, including idempotency controls, rate limiting, retries, circuit breakers, and more. The goal is to ensure stability, security, and high availability when dealing with financial transactions in a real-world payment gateway.

## Key Features

- **Clean Architecture**: Structured into layers to ensure separation of concerns, maintainability, and testability.
- **Idempotency Key**: Supports idempotent requests to avoid duplicate transactions.
- **Rate Limiting**: Implements rate limiting to prevent abuse and manage load.
- **Resiliency Patterns**: Includes retry policies, circuit breakers, and timeout handling to ensure reliability under transient failures.
- **Structured Logging**: Utilizes Serilog for detailed logging of events for observability.
- **Testing**: Comprehensive integration and unit tests to ensure functionality and correctness.
- **API Versioning**: Easy handling of different API versions to manage backward compatibility.

## Architectural Overview

### 1. **Clean Architecture**

The project follows **Clean Architecture** to enforce separation of concerns, ensure testability, and promote maintainable code. The architecture is divided into the following layers:

- **API Layer**: Contains all controllers and API endpoints. It exposes the API surface and handles HTTP requests.
- **Application Layer**: Contains business logic and application services. This layer coordinates interactions between the domain and infrastructure.
- **Domain Layer**: The core of the system, including business entities, domain models, domain services, value objects, and business rules.
- **Infrastructure Layer**: Provides the actual implementation for external services like databases, message queues, file storage, and payment processors.

### 2. **Payment Entity**

The **Payment Entity** is the core domain model that ensures data integrity and represents the state of a transaction. It is designed with the following key principles:

- **Immutable Properties**: Once a `Payment` entity is created, its properties are read-only, preventing accidental modifications. This guarantees the consistency and correctness of the payment data throughout its lifecycle.
  
### 3. **Idempotency Key**

- **Clear Separation of Concerns**: The service layer (Application) is completely separated from the data access layer (Infrastructure), ensuring that the responsibility of handling payment requests and storing/retrieving results is decoupled.
  
- **Thread-Safe In-Memory Implementation**: Uses `ConcurrentDictionary` to safely store and retrieve idempotent transaction responses, ensuring atomicity in multi-threaded environments.

- **Defensive Programming**: The code includes guard clauses to ensure inputs are validated and safe before processing.

- **Logging**: All idempotency checks, key generations, and transaction results are logged for traceability, ensuring full visibility into the request and response lifecycle.

- **Payload Integrity**: Each transaction request is hashed to verify payload integrity. If a duplicate request with the same payload is received, the system can return the same response to avoid double processing.

- **TTL (Time to Live)**: Each idempotency key is valid for a predefined period to ensure resources aren’t indefinitely stored in memory. This TTL is configurable.

### 4. **Rate Limiting**

To prevent API abuse and ensure fair usage, rate limiting is implemented at the API layer:

- If the rate limit is exceeded, a `429 Too Many Requests` response is returned.
- Configurable rate limits based on customer tiers or specific endpoints.

### 5. **Resiliency Patterns**

#### **Retry Policy (Polly)**

- Transient failures, such as network issues or temporary unavailability of third-party services, are handled with **retry policies**.
- The policy retries failed requests with **exponential backoff**. 
  - Example backoff times: 1s, 4s, and 9s.
- Ensures retries for failure conditions like timeouts, connection failures, and certain HTTP 5xx responses.

#### **Circuit Breaker**

- **Circuit Breaker** is employed to prevent the system from being overwhelmed by repeated failures and stop rapid failure cycles.
- **Threshold**: If the same failure occurs 5 times consecutively, the circuit is opened.
- **Break Duration**: Once the circuit is open, it remains open for **60 seconds** before attempting to recover.
- This prevents flooding the system with repeated failing requests.

#### **Timeout Policy**

- Requests are capped to **30 seconds**. Any request that takes longer than this duration is automatically canceled to avoid resource exhaustion and ensure system stability.
  
- **Open Circuit**: After five consecutive failures, the circuit breaker is activated for **60 seconds**, ensuring that the service can recover and avoid unnecessary overload during repeated failures.

### 6. **Structured Logging**

- **Serilog** is used for structured logging. Logs are generated for key events like request processing, transaction status, retries, circuit breaker triggers, etc.
- Logs are formatted in a structured way (JSON), enabling advanced querying and analytics, which helps in diagnosing issues and monitoring the health of the application.

### 7. **Problem Details**

To provide consistent and meaningful error responses, the API returns **Problem Details** (RFC 7807) for any errors or exceptions. This format includes:

- **Type**: A URI reference to identify the problem.
- **Title**: A short, human-readable summary of the problem.
- **Status**: HTTP status code.
- **Detail**: A detailed description of the issue.
- **Instance**: A unique identifier for the occurrence of the problem.

This structure ensures that client applications can consistently handle errors, providing a better experience for developers.

## Testing

Testing is crucial for ensuring the integrity of the payment gateway system. This project includes the following test projects:

- **PaymentGateway.Api.IntegrationTests**: Tests integration of the API layer, including end-to-end API calls and external dependencies.
- **PaymentGateway.Architecture.Tests**: Validates the architecture decisions, ensuring that the layers are correctly separated and adhere to clean architecture principles.
- **PaymentGateway.Application.UnitTests**: Contains unit tests for the application layer, focusing on the business logic, services, validation, and use cases that interact with the domain and infrastructure layers
- **PaymentGateway.Infrastructure.UnitTests**: Contains unit tests for the infrastructure layer, specifically the external API calls.
