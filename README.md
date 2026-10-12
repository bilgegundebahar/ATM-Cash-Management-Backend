# ATM Cash Management Backend — Submission

An incremental ASP.NET Core 8 REST API with Entity Framework Core and PostgreSQL. **Not all assignment requirements are implemented.**

## How to run the project and tests

1. Install Docker Desktop, then in this repository run `docker compose up --build`.
2. Open `http://localhost:8080/swagger`; check `http://localhost:8080/health`.
3. Import `ATM-Cash-Management.postman_collection.json` into Postman and run requests in order. Requests automatically reuse the created ATM ID. The reconciliation request sends `X-User: officer1`.
4. For unit tests use .NET 8 runtime/SDK and run `dotnet test tests/Atm.Tests/Atm.Tests.csproj`.
5. Database migrations run on startup via `MigrateAsync()`. The migration history is in `src/Atm.Api/Migrations`. Initial migration includes all core tables for a fresh database; the second adds `CassetteMovements`.

A clean database can be provisioned by Docker Compose's named volume on first run. **Do not use `docker compose down -v` against a database containing data you need.**

## Architecture and key design decisions

- `src/Atm.Api/Domain.cs`: ATM lifecycle, currencies, cassettes, reconciliation and movement entities.
- `AtmDb.cs`: EF Core mapping and PostgreSQL persistence.
- `NoteSolver.cs`: bounded dynamic programming for exact minimum-note withdrawal and nearest lower/upper amounts. Greedy selection is insufficient for all required cases.
- `Program.cs`: minimal REST endpoints, validation, and ProblemDetails-style errors. Some write operations use serializable DB transactions; concurrency retries are not implemented.
- Recycling cassettes contain a single currency and denomination; the deposit cassette tracks mixed notes separately. A cassette has capacity 100.
- The movement ledger table/schema is present, but normal API operations do **not** yet write complete movement history. An earlier development database used manual opening-balance records; those records are not included in the source ZIP.
- The reconciliation endpoint records discrepancies while in maintenance and requires an `X-User` header, but does not yet apply balance adjustments or enforce four-eyes approval.

## Assumptions and ambiguities

- Enum values: currencies TRY=0, USD=1, EUR=2; kinds Deposit=0, Recycling=1; states UNCONFIGURED=0, CONFIGURED=1, LOADED=2, IN_SERVICE=3, MAINTENANCE=4.
- Direct `/withdraw` requires exact cash and does not reserve banknotes or charge a per-card limit.
- Withdrawal options take an optional caller-supplied `dailyRemainingLimit`; this is not a secure card-limit implementation.
- A deposit-only cassette is untyped; mixed denominations are stored in `DepositNotes`.
- Historical manual bootstrap movement records, where present, should be understood as opening balances, not full audit history.

## Unfinished items and known limitations

- B4: quote/confirm, 60-second reservations, automatic expiry and idempotency keys.
- B5: persisted per-card daily limits and protected card identifiers.
- B6: real-database parallel request integration test and verified concurrency handling.
- B8: automatic immutable ledger writes for every cash-changing operation, and consistency-check endpoint.
- B9: per-denomination physical-count reconciliation for mixed deposit cassette, FX threshold, four-eyes approval, and applied adjustment movements.
- B10: reserved/available count reporting, TRY-converted totals, low-cash outbox and background publisher.
- Required real-database integration test has not been added. Nine NoteSolver xUnit tests were reported passing by the developer on macOS (.NET 8).
- The clean-DB migration was revised for this submission, but has **not been re-tested on a fresh Docker volume** in the packaging environment.
- The Postman collection is newly assembled and has not been executed end-to-end in the packaging environment.

## AI use

ChatGPT assisted with code scaffolding, note-selection tests, migration repair, Postman collection, and documentation. The developer ran Docker, Swagger flows and nine xUnit tests on a Mac, found and corrected an EF Core cassette-insertion error, and validated the basic ATM workflow. Newly packaged migration/Postman changes still require local verification.
