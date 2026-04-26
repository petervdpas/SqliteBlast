using SqliteBlast.Interfaces;

namespace SqliteBlast;

/// <summary>
/// Script-friendly factories. Construct an open store in one line — no DI required.
/// </summary>
/// <example>
/// <code>
/// using SqliteBlast;
///
/// using var db = SqliteBlastFactory.Open("./scratch.db");
/// db.Execute("CREATE TABLE IF NOT EXISTS notes(id INTEGER PRIMARY KEY, body TEXT)");
/// db.Execute("INSERT INTO notes(body) VALUES (@body)", new { body = "hello" });
/// var rows = db.Query&lt;Note&gt;("SELECT id, body FROM notes ORDER BY id DESC");
/// </code>
/// </example>
public static class SqliteBlastFactory
{
    /// <summary>Builds + opens a store at <paramref name="path"/>. Use <c>:memory:</c> for an in-memory database.</summary>
    public static ISqliteStore Open(string path)
    {
        var store = new SqliteStore();
        store.Setup(path);
        store.Open();
        return store;
    }

    /// <summary>Convenience: in-memory store, opened.</summary>
    public static ISqliteStore InMemory() => Open(":memory:");
}
