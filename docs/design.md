# Design Decisions

This document outlines the key architectural decisions and tradeoffs behind the Exchange Rate Service. It focuses on the reasoning behind the system’s design rather than its runtime behavior (see `overview.md` for system flow).

## Caching and Data Access Strategy

The system uses an in-memory cache per application instance, with a SQL database as the primary persistence layer. This keeps frequently accessed exchange rates readily available while ensuring historical data remains reliably stored and queryable. Cache lookups are expected to serve most requests, with the database acting as the fallback source of truth.

In a horizontally scaled environment, the in-memory cache would require a distributed implementation (e.g., Redis) to maintain consistency across instances.

## External Data Strategy (Treasury API & Ingestion)

The U.S. Treasury API is used only as a fallback when neither cache nor database contains a valid exchange rate, ensuring correctness while keeping external dependency limited to cold paths. When a rate is resolved via the Treasury fallback, the corresponding date window is queued for asynchronous ingestion. This allows missing rate data to be persisted locally so it can be served without future external calls.

A background worker handles ingestion into SQL, pre-populating the cache over time and reducing reliance on the external API during normal operation.

A scheduled job refreshes only the most recent rate window to capture Treasury amendments. As a result, newly required or recently updated rate windows may not be immediately available until background processing completes.

## Exchange Rate Window Constraint

Exchange rates are considered valid only if they fall within a six-month period prior to the transaction date.

Only rates with an effective date less than or equal to the transaction date and within this window are eligible for selection. If no valid rate exists within the window, the request fails with `EXCHANGE_RATE_NOT_FOUND`.

## Currency Mapping Strategy

Currency support is intentionally limited to a predefined subset of ISO codes mapped explicitly to Treasury descriptors via configuration. This keeps behavior predictable and avoids ambiguity in how external currency labels are interpreted.

A fully dynamic mapping system was considered but not adopted due to the complexity of tracking historical changes in Treasury currency descriptors over time.

As a future improvement, this could evolve into a versioned mapping system that preserves historical descriptor mappings, allowing the service to correctly interpret currencies across different Treasury reporting periods.


## Rate Limiting

A per-IP rate limit (30 requests per 10 seconds) is applied to protect both the database and external API from excessive load. This provides a simple safeguard suitable for a single-instance deployment. In a distributed deployment, this would require a centralized rate-limiting mechanism (e.g., Redis or an API gateway) to ensure limits are enforced consistently across all instances.


## Testing Approach

Integration tests use live Treasury API calls to validate real-world behavior of exchange rate resolution. This improves confidence in real-world behavior but introduces nondeterminism compared to fully mocked tests
