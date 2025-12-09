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

Observability
The service ships with full observability using OpenTelemetry.

Prometheus — Metrics

`http://localhost:9090`
- Scrapes metrics such as:
- Request rate
- API latency
- GC metrics
- Error counts

Grafana — Dashboards

`http://localhost:3000`

Login: `admin / admin`

Includes custom panels:
- API throughput
- Latency (p50/p90/p99)
- Error rate
- Device registration activity
- Database connection graphs
- Jaeger — Distributed Tracing

`http://localhost:16686`

Provides:
- End-to-end trace visualizations
- Handler execution spans
- Database + Redis dependency spans

Performance Notes
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






Author:

Samuel Titiloye
Software Engineer — Cloud, DevOps, Platform Engineering
SmartHome Platform Project
