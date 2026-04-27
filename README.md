# SqliteBlast 🗃️

[![NuGet](https://img.shields.io/nuget/v/SqliteBlast.svg)](https://www.nuget.org/packages/SqliteBlast)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SqliteBlast.svg)](https://www.nuget.org/packages/SqliteBlast)
[![License](https://img.shields.io/github/license/petervdpas/SqliteBlast.svg)](https://opensource.org/licenses/MIT)

![SqliteBlast](https://raw.githubusercontent.com/petervdpas/SqliteBlast/master/assets/icon.png)

**SqliteBlast** is a thin SQLite wrapper for .NET — a sibling to [SecretBlast](https://www.nuget.org/packages/SecretBlast), [AzureBlast](https://www.nuget.org/packages/AzureBlast), and [NetworkBlast](https://www.nuget.org/packages/NetworkBlast) in the Blast family. Built for scripts that need to **stage, log, or cache data locally** without dragging in a full ORM.

---

> ✅ **Status:** 1.0 — full feature set: connection management, parameterised execute / query, typed row mapping, transactions, directory-based migrations, vault-aware resolver path. 31 tests green, .NET 10.

---

## ✨ Features

* 🔹 **One-line construction** for scripts: `SqliteBlastFactory.Open("./scratch.db")` or `SqliteBlastFactory.InMemory()`
* 🔹 **Parameter binding** from POCOs *or* dictionaries — `db.Execute(sql, new { id = 7 })`
* 🔹 **Typed row mapping** — `db.Query<Note>("SELECT id, body FROM notes")` reflects properties, coerces types (DateTime, Guid, bool, enum, byte[], nullables)
* 🔹 **Transactions** with safe-by-default rollback semantics (commit explicitly to keep changes)
* 🔹 **Migrations** — drop `*.sql` files in a folder, call `RunMigrations(dir)`; tracked in `__migrations__`, applied in lexical order, idempotent
* 🔹 **Vault-aware resolver path** — same `Func<category, key, ct, Task<string>>` shape as AzureBlast 2.1; pass `Secrets.Resolver` from TaskBlaster / SecretBlast
* 🔹 **Tiny dep graph** — only `Microsoft.Data.Sqlite` and `Microsoft.Extensions.DependencyInjection.Abstractions`

---

## 📦 Installation

```bash
dotnet add package SqliteBlast
```

---

## 🚀 Script Quick-Start

```csharp
#r "nuget: SqliteBlast, 1.0.0"
using SqliteBlast;

using var db = SqliteBlastFactory.Open("./notes.db");

db.Execute("""
    CREATE TABLE IF NOT EXISTS notes (
        id   INTEGER PRIMARY KEY AUTOINCREMENT,
        body TEXT NOT NULL,
        seen INTEGER NOT NULL DEFAULT 0
    );
    """);

db.Execute("INSERT INTO notes(body) VALUES (@body)", new { body = "first" });
db.Execute("INSERT INTO notes(body) VALUES (@body)", new { body = "second" });

foreach (var note in db.Query<Note>("SELECT id, body, seen FROM notes ORDER BY id"))
    Console.WriteLine($"#{note.Id} {note.Body} (seen={note.Seen})");

record Note { public long Id { get; set; } public string Body { get; set; } = ""; public bool Seen { get; set; } }
```

### In-memory store

```csharp
using var db = SqliteBlastFactory.InMemory();
db.Execute("CREATE TABLE t(x INT)");
db.Execute("INSERT INTO t(x) VALUES (1), (2), (3)");
var sum = db.ExecuteScalar<long>("SELECT SUM(x) FROM t");  // 6
```

---

## 🔁 Transactions

Default behaviour on dispose is **rollback** — commit explicitly to keep changes:

```csharp
using (var tx = db.BeginTransaction())
{
    db.Execute("UPDATE accounts SET balance = balance - @amount WHERE id = @from", new { amount = 50, from = 1 });
    db.Execute("UPDATE accounts SET balance = balance + @amount WHERE id = @to",   new { amount = 50, to   = 2 });
    tx.Commit();
}
// If an exception is thrown above, the using-dispose rolls back automatically.
```

---

## 📂 Migrations

Drop `*.sql` files in a directory; SqliteBlast applies the new ones in lexical order, tracked in a `__migrations__` table:

```
migrations/
  001_init.sql
  002_add_seen.sql
  003_indexes.sql
```

```csharp
var applied = db.RunMigrations("./migrations");   // returns N (newly-applied)
```

Each file runs inside a transaction; a failed migration rolls back so the table stays consistent.

---

## 🧱 DI Wiring

```csharp
using SqliteBlast;
using Microsoft.Extensions.DependencyInjection;

services.AddSqliteBlast(o =>
{
    o.DatabasePath        = "./scratch.db";
    o.MigrationsDirectory = "./migrations";   // optional — runs on first resolution
});
```

### Resolver-driven (vault-backed path)

```csharp
services.AddSqliteBlast(o =>
{
    o.Resolver       = Secrets.Resolver;      // any Func<category, key, ct, Task<string>>
    o.ConnectionName = "scratch";             // → resolver looks up (scratch, "path")
    // o.PathKey      = "path";                // override if your vault uses a different field
});
```

The resolver path takes precedence over `DatabasePath` when both are configured for the same store.

---

## 📖 API Surface

```csharp
namespace SqliteBlast.Interfaces;

public interface ISqliteStore : IDisposable
{
    string? DatabasePath { get; }
    bool    IsOpen       { get; }

    void Setup(string path);
    void Open();
    void Close();

    int          Execute        (string sql, object? parameters = null);
    T?           ExecuteScalar<T>(string sql, object? parameters = null);
    IReadOnlyList<T> Query<T>   (string sql, object? parameters = null) where T : new();
    DataTable    QueryDataTable (string sql, object? parameters = null);

    ISqliteTransactionScope BeginTransaction();
    int RunMigrations(string migrationsDirectory);
}

public interface ISqliteTransactionScope : IDisposable
{
    void Commit();
}

namespace SqliteBlast;

public sealed class SqliteStore : ISqliteStore { /* implementation */ }

public static class SqliteBlastFactory
{
    public static ISqliteStore Open(string path);
    public static ISqliteStore InMemory();
}

public static class SqliteBlastResolverExtensions
{
    public static Task SetupAsync(
        this ISqliteStore store,
        Func<string, string, CancellationToken, Task<string>> resolver,
        string connectionName,
        string pathKey = "path",
        CancellationToken cancellationToken = default);
}

public class SqliteBlastOptions
{
    public string? DatabasePath        { get; set; }
    public Func<string, string, CancellationToken, Task<string>>? Resolver { get; set; }
    public string? ConnectionName      { get; set; }
    public string  PathKey             { get; set; } = "path";
    public string? MigrationsDirectory { get; set; }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteBlast(
        this IServiceCollection services,
        Action<SqliteBlastOptions> configure);
}
```

---

## 🤖 AI assistants

This assembly carries the **Blast.PrimaryFacade** convention: an
`[AssemblyMetadata("Blast.PrimaryFacade", "...")]` attribute names the
canonical front-door type(s) of the package, so AI helpers (e.g.
TaskBlaster's script assistant) can identify the entry points without
scanning every public type.

For SqliteBlast the front doors are:

| Type | Purpose |
|------|---------|
| `SqliteBlast.SqliteStore` | The store interface: `Execute`, `Query<T>`, `BeginTransaction`. |
| `SqliteBlast.SqliteBlastFactory` | One-line factories: `Open(path)`, `InMemory()`. |

Read it back from a loaded assembly via reflection:

```csharp
var facade = typeof(SqliteBlast.SqliteStore).Assembly
    .GetCustomAttributes<AssemblyMetadataAttribute>()
    .FirstOrDefault(a => a.Key == "Blast.PrimaryFacade")?.Value;
```

The value is a hint for tooling; consumers don't need to read it.

---

## 📜 License

[MIT](https://opensource.org/licenses/MIT)
