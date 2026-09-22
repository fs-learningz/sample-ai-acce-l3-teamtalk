# TeamTalk.DataMigration

One-off tool that copies existing rows from the retired SQL Server database into the new
PostgreSQL database, using two `TeamTalkDbContext` instances (source on the SqlServer
provider, target on Npgsql) instead of a DB-native export/import tool. It is not part of the
running application and is not referenced by `TeamTalk.slnx` — **delete this folder once the
cutover is verified.**

## Prerequisites

1. The target PostgreSQL database exists and the current EF Core migrations are applied to it:
   ```
   dotnet ef database update \
     --project src/TeamTalk.Infrastructure/TeamTalk.Infrastructure.csproj \
     --startup-project src/TeamTalk.Api/TeamTalk.Api.csproj
   ```
   (point `ConnectionStrings__teamtalkdb` at the target Postgres instance first). This tool
   checks for at least one applied migration on the target and refuses to run otherwise — it
   only copies rows, it does not create schema.
2. The SQL Server source is reachable and read-only access is enough (the tool never writes
   to it).

## Usage

```
export SOURCE_CONNECTION_STRING="Server=<sql-server-host>;Database=teamtalkdb;User Id=...;Password=...;TrustServerCertificate=True"
export TARGET_CONNECTION_STRING="Host=<postgres-host>;Database=teamtalkdb;Username=...;Password=..."
dotnet run --project tools/TeamTalk.DataMigration
```

Both variables hold credentials — set them directly in the shell/CI secret store, never commit
them, and unset them (or close the shell) once the run finishes.

The tool copies tables in foreign-key order, batches writes (500 rows/`SaveChanges`), and
prints a source-vs-target row count per table plus a timestamp spot check at the end. A
non-zero exit code means at least one table's counts didn't match — treat that as a blocker,
not a warning.

## Downtime strategy and cutover window

This is a one-time, offline batch copy, not continuous replication. Run it during a
maintenance window with the application stopped (or in read-only mode) against the SQL Server
source, so no rows change after the copy starts. There is no CDC/dual-write path here — if the
app keeps taking writes against SQL Server during the copy, those writes will not appear in
PostgreSQL.

Order of operations for the cutover:
1. Stop write traffic to the application (maintenance page or read-only mode).
2. Run this tool.
3. Confirm the verification output shows no mismatches.
4. Point the app's connection string at PostgreSQL (already the default after this migration)
   and restart it.
5. Smoke test, then resume write traffic.

## Rollback plan

The SQL Server source is never modified by this tool, so rolling back is: repoint the app's
connection string/Aspire resource back at SQL Server and restart. Keep the SQL Server instance
running and untouched until PostgreSQL has been verified in production for a reasonable burn-in
period — don't decommission it on cutover day.
