using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using SqliteBlast.Interfaces;
using SqliteBlast.Internal;
using SqliteBlast.Migrations;

namespace SqliteBlast;

/// <summary>
/// Default <see cref="ISqliteStore"/> implementation. Owns a single
/// <see cref="SqliteConnection"/>; opens lazily on first query when not
/// already opened.
/// </summary>
public sealed class SqliteStore : ISqliteStore
{
    private SqliteConnection? _connection;
    private string? _path;
    private bool _disposed;

    /// <inheritdoc/>
    public string? DatabasePath => _path;

    /// <inheritdoc/>
    public bool IsOpen => _connection is { State: ConnectionState.Open };

    /// <inheritdoc/>
    public void Setup(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    /// <inheritdoc/>
    public void Open()
    {
        ThrowIfDisposed();
        if (_path is null) throw new InvalidOperationException("Call Setup(path) before Open().");
        if (IsOpen) return;

        _connection?.Dispose();
        _connection = new SqliteConnection($"Data Source={_path}");
        _connection.Open();
    }

    /// <inheritdoc/>
    public void Close()
    {
        if (_connection is { State: not ConnectionState.Closed }) _connection.Close();
    }

    /// <inheritdoc/>
    public int Execute(string sql, object? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        EnsureOpen();
        using var command = CreateCommand(sql, parameters);
        return command.ExecuteNonQuery();
    }

    /// <inheritdoc/>
    public T? ExecuteScalar<T>(string sql, object? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        EnsureOpen();
        using var command = CreateCommand(sql, parameters);
        var raw = command.ExecuteScalar();
        if (raw is null || raw == DBNull.Value) return default;

        var underlying = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert.ChangeType(raw, underlying, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public IReadOnlyList<T> Query<T>(string sql, object? parameters = null) where T : new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        EnsureOpen();
        using var command = CreateCommand(sql, parameters);
        using var reader = command.ExecuteReader();

        var results = new List<T>();
        while (reader.Read())
            results.Add(RowMapper.Map<T>(reader));
        return results;
    }

    /// <inheritdoc/>
    public DataTable QueryDataTable(string sql, object? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        EnsureOpen();
        using var command = CreateCommand(sql, parameters);
        using var reader = command.ExecuteReader();

        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    /// <inheritdoc/>
    public ISqliteTransactionScope BeginTransaction()
    {
        EnsureOpen();
        return new TransactionScope(_connection!.BeginTransaction());
    }

    /// <inheritdoc/>
    public int RunMigrations(string migrationsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(migrationsDirectory);
        if (!Directory.Exists(migrationsDirectory))
            throw new DirectoryNotFoundException($"Migrations directory not found: {migrationsDirectory}");

        EnsureOpen();
        return MigrationRunner.Apply(this, migrationsDirectory);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _connection?.Dispose();
        _connection = null;
        _disposed = true;
    }

    // ---------- internals ----------

    private void EnsureOpen()
    {
        ThrowIfDisposed();
        if (!IsOpen) Open();
    }

    private SqliteCommand CreateCommand(string sql, object? parameters)
    {
        var command = _connection!.CreateCommand();
        command.CommandText = sql;
        ParameterBinder.Bind(command, parameters);
        return command;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SqliteStore));
    }

    private sealed class TransactionScope : ISqliteTransactionScope
    {
        private SqliteTransaction? _transaction;
        private bool _committed;

        public TransactionScope(SqliteTransaction transaction)
        {
            _transaction = transaction;
        }

        public void Commit()
        {
            if (_transaction is null) return;
            _transaction.Commit();
            _committed = true;
            _transaction.Dispose();
            _transaction = null;
        }

        public void Dispose()
        {
            if (_transaction is null) return;
            if (!_committed) _transaction.Rollback();
            _transaction.Dispose();
            _transaction = null;
        }
    }
}
