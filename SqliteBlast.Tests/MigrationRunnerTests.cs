using System;
using System.IO;
using SqliteBlast;
using Xunit;

namespace SqliteBlast.Tests;

public class MigrationRunnerTests : IDisposable
{
    private readonly string _dir;
    private readonly string _dbPath;

    public MigrationRunnerTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "SqliteBlast_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _dbPath = Path.Combine(_dir, "test.db");
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ }
    }

    private void WriteMigration(string name, string sql) =>
        File.WriteAllText(Path.Combine(_dir, name), sql);

    [Fact]
    public void RunMigrations_AppliesAllFilesInLexicalOrder()
    {
        WriteMigration("001_init.sql",      "CREATE TABLE notes(id INTEGER PRIMARY KEY, body TEXT NOT NULL);");
        WriteMigration("002_add_seen.sql",  "ALTER TABLE notes ADD COLUMN seen INTEGER NOT NULL DEFAULT 0;");

        using var s = SqliteBlastFactory.Open(_dbPath);
        var applied = s.RunMigrations(_dir);

        Assert.Equal(2, applied);
        s.Execute("INSERT INTO notes(body, seen) VALUES ('hi', 1)");
        Assert.Equal(1L, s.ExecuteScalar<long>("SELECT seen FROM notes"));
    }

    [Fact]
    public void RunMigrations_SecondRun_AppliesNothing_AndDoesNotReapply()
    {
        WriteMigration("001_init.sql", "CREATE TABLE notes(id INTEGER PRIMARY KEY, body TEXT NOT NULL);");

        using (var s = SqliteBlastFactory.Open(_dbPath))
        {
            Assert.Equal(1, s.RunMigrations(_dir));
            Assert.Equal(0, s.RunMigrations(_dir));   // idempotent
        }

        // New process / new connection — still tracked.
        using (var s = SqliteBlastFactory.Open(_dbPath))
            Assert.Equal(0, s.RunMigrations(_dir));
    }

    [Fact]
    public void RunMigrations_NewFileAddedLater_OnlyAppliesNewOne()
    {
        WriteMigration("001_init.sql", "CREATE TABLE notes(id INTEGER PRIMARY KEY, body TEXT NOT NULL);");
        using (var s = SqliteBlastFactory.Open(_dbPath))
            Assert.Equal(1, s.RunMigrations(_dir));

        WriteMigration("002_add_seen.sql", "ALTER TABLE notes ADD COLUMN seen INTEGER NOT NULL DEFAULT 0;");
        using (var s = SqliteBlastFactory.Open(_dbPath))
            Assert.Equal(1, s.RunMigrations(_dir));   // only the new one
    }

    [Fact]
    public void RunMigrations_EmptyDirectory_AppliesZero()
    {
        using var s = SqliteBlastFactory.Open(_dbPath);
        Assert.Equal(0, s.RunMigrations(_dir));
    }

    [Fact]
    public void RunMigrations_NonExistentDirectory_Throws()
    {
        using var s = SqliteBlastFactory.InMemory();
        Assert.Throws<DirectoryNotFoundException>(() => s.RunMigrations(Path.Combine(_dir, "missing")));
    }
}
