# Exchange Rate Service

Backend API for recording purchases in USD and converting them into foreign currencies using historical exchange rates from the [U.S. Treasury Reporting Rates of Exchange API](https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange).

Each transaction is converted using a historical exchange rate selected based on the purchase date. The system uses a tiered resolution strategy and background ingestion to reduce reliance on external requests and improve lookup performance.

## Features

- Record purchases purchases in USD
- Convert purchases into supported currencies
- Historical exchange rate lookup
- Multi-tier caching and fallback strategy
- Background ingestion pipeline for rate data and ammendments
- Structured API error responses
- Swagger UI for interactive API exploration

## API Overview

Base URL: `http://localhost:8080` (Swagger UI available at `/` in development)

| Method | Endpoint                                               | Description                       |
| ------ | ------------------------------------------------------ | --------------------------------- |
| GET    | `/api/v1/currencies`                                   | List supported ISO currency codes |
| GET    | `/api/v1/transactions?page={page}&pageSize={pageSize}` | List transactions                 |
| GET    | `/api/v1/transactions/{id}`                            | Get transaction by ID             |
| POST   | `/api/v1/transactions`                                 | Create a transaction              |
| GET    | `/api/v1/transactions/{id}/convert?currencyCode=CAD`   | Convert transaction currency      |

> Tip: Use `/api/v1/currencies` before converting to ensure the service supports the requested currency.

## Running Locally

### Prerequisites

- Docker + Docker Compose
- .NET 10 SDK (optional, for running tests locally)

### Run with Docker Compose

An example `.env` file has been provided with preconfigured credentials for purely development purposes.

```bash
cp .env.example .env
docker compose up --build
```

## Testing

To run automated test suite:

```bash
dotnet test
```

## Documentation

See the full system documentation for deeper technical details:

- [System Overview](docs/overview.md) — architecture and request flow
- [Design Decisions](docs/design.md) — tradeoffs and assumptions