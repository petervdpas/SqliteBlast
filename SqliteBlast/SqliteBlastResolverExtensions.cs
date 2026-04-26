using System;
using System.Threading;
using System.Threading.Tasks;
using SqliteBlast.Interfaces;

namespace SqliteBlast;

/// <summary>
/// Resolver-aware overloads that mirror the AzureBlast 2.1 pattern: pull the
/// SQLite path through a <c>Func&lt;category, key, ct, Task&lt;string&gt;&gt;</c>
/// resolver delegate (typically <c>Secrets.Resolver</c> from TaskBlaster /
/// SecretBlast) and forward to <see cref="ISqliteStore.Setup(string)"/>.
/// </summary>
public static class SqliteBlastResolverExtensions
{
    /// <summary>
    /// Pulls the database path from the supplied resolver and forwards to
    /// <see cref="ISqliteStore.Setup(string)"/>.
    /// </summary>
    /// <param name="store">The store to configure.</param>
    /// <param name="resolver">Secret resolver delegate.</param>
    /// <param name="connectionName">Logical connection name (the resolver category).</param>
    /// <param name="pathKey">Key holding the SQLite path. Defaults to <c>path</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task SetupAsync(
        this ISqliteStore store,
        Func<string, string, CancellationToken, Task<string>> resolver,
        string connectionName,
        string pathKey = "path",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        var path = await resolver(connectionName, pathKey, cancellationToken).ConfigureAwait(false);
        store.Setup(path);
    }
}
