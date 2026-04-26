using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SqliteBlast.Interfaces;

namespace SqliteBlast;

/// <summary>
/// DI registration helpers for SqliteBlast.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="ISqliteStore"/> singleton configured via
    /// <see cref="SqliteBlastOptions"/>. The resolver path takes precedence over
    /// <see cref="SqliteBlastOptions.DatabasePath"/> when both are set.
    /// </summary>
    public static IServiceCollection AddSqliteBlast(this IServiceCollection services, Action<SqliteBlastOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SqliteBlastOptions();
        configure(options);

        services.TryAddSingleton<ISqliteStore>(_ =>
        {
            var store = new SqliteStore();

            if (options.Resolver is not null && !string.IsNullOrWhiteSpace(options.ConnectionName))
            {
                store.SetupAsync(options.Resolver, options.ConnectionName!, options.PathKey).GetAwaiter().GetResult();
            }
            else if (!string.IsNullOrWhiteSpace(options.DatabasePath))
            {
                store.Setup(options.DatabasePath!);
            }
            else
            {
                throw new InvalidOperationException(
                    "SqliteBlast: configure either DatabasePath, or Resolver + ConnectionName.");
            }

            store.Open();
            if (!string.IsNullOrWhiteSpace(options.MigrationsDirectory))
                store.RunMigrations(options.MigrationsDirectory!);
            return store;
        });

        return services;
    }
}
