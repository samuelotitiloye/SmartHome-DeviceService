SmartHome DeviceService

DeviceService is a production-grade .NET 8 microservice responsible for managing smart-home IoT devices, including registration, updates, queries, filtering, pagination, and health visibility.
It is built using Clean Architecture, CQRS/MediatR, EF Core, Redis caching, OpenTelemetry, Serilog, and a full Docker Compose observability stack.

This service is part of the larger SmartHome Platform (DeviceService → EnergyDataService → AlertService).

Architecture Diagram
Placeholder — diagram
```
flowchart
    LAYERS
    A[DeviceService.Api<br/>ASP.NET Minimal API · OTel · Rate Limiting]:::layer
    B[MediatR (CQRS)<br/>Commands · Queries]:::layer
    C[DeviceService.Application<br/>Validators · Behaviors]:::layer
    D[DeviceService.Domain<br/>Entities · Enums · Abstractions]:::layer
    E[DeviceService.Infrastructure<br/>EF Core · Repositories · Redis Cache]:::infra

    STORAGE
    F[(PostgreSQL)]:::storage
    G[(Redis Cache)]:::storage

    FLOWS
    A --> B
    B --> C
    C --> D
    D --> E
    E --> F
    E --> G
```

Request Flow (High-Level)
Client
  → API Endpoint
    → MediatR Command/Query
      → Validation Pipeline
      → Handler Executes
         → Repository → PostgreSQL
         → Redis Cache (Reads/Writes)
  → Response returned
  → OpenTelemetry sends metrics + traces

Detailed Request Flow:
 - Minimal API → MediatR → Handler → DB/Cache
  ```flowchart LR
    A[HTTP Endpoint<br/> /api/devices/register] --> B[Request DTO]
    B --> C[MediatR Command]
    C --> D[Handler]
    D --> E[Repository]
    E --> F[(PostgreSQL DB)]
    D --> G[(Redis Cache)]
```
- Full Behavior Sequence (including Redis caching)
```
sequenceDiagram
    autonumber
    participant C as Client
    participant API as DeviceService.Api
    participant M as MediatR Pipeline
    participant H as Handler
    participant R as Repository
    participant DB as PostgreSQL
    participant RED as Redis Cache

    C->>API: HTTP Request (e.g., GET /devices/{id})
    API->>M: Create Query/Command
    M->>H: Run Validators · Logging · Behaviors
    alt Is Cached?
        H->>RED: Check Cache
        RED-->>H: Cached Item Found
        H-->>API: Return Cached Response
        API-->>C: 200 OK
    else Not Cached
        H->>R: Query Database
        R->>DB: SELECT/INSERT/UPDATE
        DB-->>R: Result
        R-->>H: Mapped DTO
        H->>RED: Write to Cache
        H-->>API: Handler Result
        API-->>C: 200 OK (from DB)
    end
```


Docker Instructions:
Start the entire stack
`docker compose up --build`

Stop / remove containers
`docker compose down`

Services included in Docker Compose:
- DeviceService API
- PostgreSQL
- Redis
- Prometheus
- Grafana
- Jaeger
- cAdvisor

This stack gives you full API functionality, caching, distributed tracing, and metrics dashboards out of the box.

Environment Variables

A .env.example file is included in the repository.
```
| Variable                      | Description             | Example                      |
| ----------------------------- | ----------------------- | ---------------------------- |
| `DB_HOST`                     | PostgreSQL host         | `postgres`                   |
| `DB_PORT`                     | PostgreSQL port         | `5432`                       |
| `DB_USER`                     | DB username             | `postgres`                   |
| `DB_PASSWORD`                 | DB password             | `postgres`                   |
| `DB_NAME`                     | Device DB name          | `devicesdb`                  |
| `REDIS_CONNECTION`            | Redis connection string | `redis:6379`                 |
| `ASPNETCORE_ENVIRONMENT`      | Runtime environment     | `Development`                |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Collector endpoint      | `http://otel-collector:4317` |
| `RATE_LIMIT_PER_MINUTE`       | API rate limit          | `30`                         |
```
Testing Instructions
Run all tests:
`dotnet test`

Run tests for one project
`dotnet test DeviceService.Tests`

The test suite includes:
- Handler-level unit tests
- Mocked repository + services
- In-memory EF Core patterns
- Request filtering and pagination tests
- Error-handling and validation tests
All tests run in CI via GitHub Actions.

## Observability

DeviceService ships with full observability using OpenTelemetry, providing metrics, dashboards, and distributed tracing out of the box.

### Prometheus — Metrics
http://localhost:9090

Scrapes and stores service metrics including:
- Request rate (RPS)
- API latency (p90 / p99)
- Error counts
- Runtime & GC metrics

### Grafana — Dashboards
http://localhost:3000  
Login: `admin / admin`

Includes custom dashboards for:
- API throughput
- Latency (p90 / p99)
- Error rate (handles zero-error traffic correctly)
- Health & readiness signals

### Jaeger — Distributed Tracing
http://localhost:16686

Provides:
- End-to-end request tracing
- MediatR handler execution spans
- Database and Redis dependency spans


Example API Requests (cURL):
 - Here are sample cURL commands demonstrating the core device workflows.
Register a Device
 ```
 curl -X POST http://localhost:8080/api/devices/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Kitchen Sensor",
    "type": "sensor",
    "location": "Kitchen",
    "isOnline": true,
    "thresholdWatts": 50,
    "serialNumber": "ABC12345"
  }'
```
Get Device by ID

`curl http://localhost:8080/api/devices/<deviceId>`

Update A Device
```
curl -X PUT http://localhost:8080/api/devices/<deviceId> \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Kitchen Sensor v2",
    "type": "sensor",
    "location": "Kitchen",
    "isOnline": false,
    "thresholdWatts": 45,
    "serialNumber": "ABC12345"
  }'
```

List Devices (with pagination + filters)
`curl "http://localhost:8080/api/devices?page=1&pageSize=10&nameContains=sensor&location=Kitchen"`

Delete A Device
`curl -X DELETE http://localhost:8080/api/devices/<deviceId>`

Health Check
`curl http://localhost:8080/health`

Prometheus Metrics
`curl http://localhost:8080/metrics`

Redis Connectivity Test (via container)
`docker exec -it redis redis-cli ping`

Performance Notes:
- Redis caching significantly reduces load on GET /api/devices/{id}.
- Pagination protects the DB by limiting large reads.
- Global rate limiting prevents noisy clients from overwhelming the API.
- Response caching enabled for specific endpoints.
- EF Core NoTracking improves performance for read-only queries.
- Bulk operations optimized via batch inserts/updates where applicable.

Future Expansion:
EnergyDataService (next microservice)
- Receives device energy consumption events
- Computes real-time usage metrics
- Integrates with DeviceService through device IDs
- Long-term: energy anomaly detection

AlertService
- Monitors offline/high-wattage devices
- Sends alerts via SNS/email/webhooks
- Uses Redis or DynamoDB for alert state tracking

Both services will use the same Clean Architecture, observability stack, and infrastructure patterns.


For Recruiters & Interviewers
This repository showcases ability to build production-grade backend services with modern engineering practices:

Key Skills Demonstrated:
- Clean Architecture with strict layering
- CQRS + MediatR for maintainability and separation of concerns
- Redis caching for performance optimization
- Resilient API design with rate limiting & response caching
- Comprehensive OpenTelemetry instrumentation (metrics + traces)
- Containerized local environment (PostgreSQL, Redis, Prometheus, Grafana, Jaeger)
- Full unit test suite for business logic and handlers
- Developer-friendly documentation and examples

Why This Project Stands Out:
This service mirrors patterns used in cloud-native companies and platform engineering teams:
- Observable
- Testable
- Scalable
- Containerized
- Portable

It also demonstrates the ability to:
- Work iteratively using sprint-based development
- Produce clear documentation
- Write clean, maintainable C# and .NET 8 code
- Think like a backend engineer and DevOps engineer

What to Look At:
`/DeviceService.Api` for minimal APIs, rate limiting, and OTel setup
`/DeviceService.Application` for commands, queries, handlers, and validations
`/DeviceService.Infrastructure` for EF Core, caching, and repository patterns
`/DeviceService.Tests` for modular unit test design
`docker-compose.yml` for local cloud-style environment
Grafana dashboards + Prometheus metrics via the observability stack





Author:

`Samuel Titiloye`

`Software Engineer — Cloud, DevOps, Platform Engineering`

`SmartHome Platform Project`
