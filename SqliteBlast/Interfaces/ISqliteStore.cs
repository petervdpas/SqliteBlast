using System;
using System.Collections.Generic;
using System.Data;

namespace SqliteBlast.Interfaces;

/// <summary>
/// Local SQLite store: connection management, parameterised execution,
/// typed query results, transactions, and migrations.
/// </summary>
public interface ISqliteStore : IDisposable
{
    /// <summary>Path to the SQLite file (or <c>:memory:</c>). <c>null</c> until <see cref="Setup(string)"/>.</summary>
    string? DatabasePath { get; }

    /// <summary><c>true</c> when the underlying connection is open.</summary>
    bool IsOpen { get; }

    /// <summary>Sets the database path. Call before <see cref="Open"/>.</summary>
    void Setup(string path);

    /// <summary>Opens the underlying <c>SqliteConnection</c>. Idempotent.</summary>
    void Open();

    /// <summary>Closes the underlying connection.</summary>
    void Close();

    /// <summary>
    /// Executes a non-query SQL statement (INSERT/UPDATE/DELETE/CREATE/...).
    /// Parameters can be a dictionary or a POCO whose readable public properties
    /// map to <c>@PropertyName</c> placeholders.
    /// </summary>
    /// <returns>The number of rows affected, when reported by SQLite.</returns>
    int Execute(string sql, object? parameters = null);

    /// <summary>Executes a query and returns the first column of the first row coerced to <typeparamref name="T"/>.</summary>
    T? ExecuteScalar<T>(string sql, object? parameters = null);

    /// <summary>
    /// Executes a query and maps each row to a new instance of <typeparamref name="T"/>
    /// by writing matching columns into public settable properties.
    /// Type coercion handles strings, the common numeric types, <see cref="bool"/>,
    /// <see cref="DateTime"/>, <see cref="Guid"/>, <see cref="byte"/>[] and nullables.
    /// </summary>
    IReadOnlyList<T> Query<T>(string sql, object? parameters = null) where T : new();

    /// <summary>Executes a query and returns a <see cref="DataTable"/> with the result set.</summary>
    DataTable QueryDataTable(string sql, object? parameters = null);

    /// <summary>Begins a transaction. Dispose to roll back; call <c>Commit()</c> on the returned scope to keep the changes.</summary>
    ISqliteTransactionScope BeginTransaction();

    /// <summary>
    /// Applies every <c>*.sql</c> file in <paramref name="migrationsDirectory"/> that hasn't been applied yet,
    /// in lexical order. Tracks applied migrations in a <c>__migrations__</c> table.
    /// </summary>
    /// <returns>The number of newly-applied migrations.</returns>
    int RunMigrations(string migrationsDirectory);
}

/// <summary>Disposable scope returned by <see cref="ISqliteStore.BeginTransaction"/>. Default behaviour on dispose is rollback.</summary>
public interface ISqliteTransactionScope : IDisposable
{
    /// <summary>Commit the transaction. After commit, dispose is a no-op.</summary>
    void Commit();
}
