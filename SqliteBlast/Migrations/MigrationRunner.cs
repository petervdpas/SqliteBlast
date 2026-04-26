using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SqliteBlast.Interfaces;

namespace SqliteBlast.Migrations;

/// <summary>
/// Applies <c>*.sql</c> files from a directory in lexical order. Tracks applied
/// migrations in a <c>__migrations__</c> table so each file is run at most once.
/// </summary>
internal static class MigrationRunner
{
    public static int Apply(ISqliteStore store, string directory)
    {
        store.Execute("""
            CREATE TABLE IF NOT EXISTS __migrations__ (
                name        TEXT PRIMARY KEY,
                applied_utc TEXT NOT NULL
            );
            """);

        var alreadyApplied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in store.Query<MigrationRow>("SELECT name FROM __migrations__"))
            if (row.Name is not null) alreadyApplied.Add(row.Name);

        var files = Directory
            .EnumerateFiles(directory, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(p => Path.GetFileName(p), StringComparer.Ordinal)
            .ToList();

        var applied = 0;
        foreach (var file in files)
        {
            var name = Path.GetFileName(file);
            if (alreadyApplied.Contains(name)) continue;

            var script = File.ReadAllText(file);
            using var tx = store.BeginTransaction();
            store.Execute(script);
            store.Execute(
                "INSERT INTO __migrations__ (name, applied_utc) VALUES (@name, @applied_utc)",
                new { name, applied_utc = DateTime.UtcNow.ToString("O") });
            tx.Commit();
            applied++;
        }
        return applied;
    }

    private sealed class MigrationRow
    {
        public string? Name { get; set; }
    }
}
