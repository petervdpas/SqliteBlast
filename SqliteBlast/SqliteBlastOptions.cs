using System;
using System.Threading;
using System.Threading.Tasks;

namespace SqliteBlast;

/// <summary>
/// Options used to register a <c>SqliteStore</c> with the DI container.
/// </summary>
/// <remarks>
/// Provide <see cref="DatabasePath"/> for the direct path setup, OR
/// <see cref="Resolver"/> + <see cref="ConnectionName"/> to fetch the path from a vault
/// (e.g. <c>Secrets.Resolver</c> from TaskBlaster / SecretBlast). When both are set,
/// the resolver path wins.
/// </remarks>
public class SqliteBlastOptions
{
    /// <summary>Direct SQLite path (or <c>:memory:</c>). Used when no resolver is configured.</summary>
    public string? DatabasePath { get; set; }

    /// <summary>Optional secret resolver delegate; when set together with <see cref="ConnectionName"/>, fetches the path from a vault.</summary>
    public Func<string, string, CancellationToken, Task<string>>? Resolver { get; set; }

    /// <summary>Logical connection name used by the resolver to fetch the database path.</summary>
    public string? ConnectionName { get; set; }

    /// <summary>Resolver key holding the database path. Defaults to <c>path</c>.</summary>
    public string PathKey { get; set; } = "path";

    /// <summary>Optional migrations directory; when set, <c>RunMigrations</c> is called on first resolution.</summary>
    public string? MigrationsDirectory { get; set; }
}
