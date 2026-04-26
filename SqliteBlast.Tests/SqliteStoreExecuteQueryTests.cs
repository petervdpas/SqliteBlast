using System;
using System.Collections.Generic;
using SqliteBlast;
using SqliteBlast.Interfaces;
using Xunit;

namespace SqliteBlast.Tests;

public class SqliteStoreExecuteQueryTests
{
    private sealed class Person
    {
        public long   Id        { get; set; }
        public string Name      { get; set; } = "";
        public int    Age       { get; set; }
        public bool   IsActive  { get; set; }
        public Guid   Token     { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? Note     { get; set; }
    }

    private static ISqliteStore Build()
    {
        var s = SqliteBlastFactory.InMemory();
        s.Execute("""
            CREATE TABLE people (
                id         INTEGER PRIMARY KEY AUTOINCREMENT,
                name       TEXT    NOT NULL,
                age        INTEGER NOT NULL,
                is_active  INTEGER NOT NULL,
                token      TEXT    NOT NULL,
                created_on TEXT    NOT NULL,
                note       TEXT
            );
            """);
        return s;
    }

    [Fact]
    public void Execute_ReturnsAffectedRowCount()
    {
        using var s = Build();
        var inserted = s.Execute(
            "INSERT INTO people(name, age, is_active, token, created_on) VALUES (@Name, @Age, @IsActive, @Token, @CreatedOn)",
            new Person { Name = "Alice", Age = 30, IsActive = true, Token = Guid.NewGuid(), CreatedOn = DateTime.UtcNow });
        Assert.Equal(1, inserted);
    }

    [Fact]
    public void ExecuteScalar_CoercesToTargetType()
    {
        using var s = Build();
        s.Execute("INSERT INTO people(name, age, is_active, token, created_on) VALUES ('a', 10, 1, '', '')");
        s.Execute("INSERT INTO people(name, age, is_active, token, created_on) VALUES ('b', 20, 1, '', '')");

        var sum = s.ExecuteScalar<long>("SELECT SUM(age) FROM people");
        Assert.Equal(30, sum);
    }

    [Fact]
    public void ExecuteScalar_NoRows_ReturnsDefault()
    {
        using var s = Build();
        var name = s.ExecuteScalar<string>("SELECT name FROM people WHERE id = 999");
        Assert.Null(name);
    }

    [Fact]
    public void Query_MapsTypedRows_IncludingTypeCoercions()
    {
        using var s = Build();
        var token = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var when = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc);

        s.Execute(
            "INSERT INTO people(name, age, is_active, token, created_on, note) VALUES (@Name, @Age, @IsActive, @Token, @CreatedOn, @Note)",
            new Person { Name = "Alice", Age = 30, IsActive = true, Token = token, CreatedOn = when, Note = null });

        var rows = s.Query<Person>("SELECT id, name, age, is_active AS IsActive, token, created_on AS CreatedOn, note FROM people");

        Assert.Single(rows);
        var p = rows[0];
        Assert.Equal("Alice", p.Name);
        Assert.Equal(30, p.Age);
        Assert.True(p.IsActive);
        Assert.Equal(token, p.Token);
        Assert.Equal(when, p.CreatedOn);
        Assert.Null(p.Note);
    }

    [Fact]
    public void Query_DictionaryParameters_ArePassedThrough()
    {
        using var s = Build();
        s.Execute("INSERT INTO people(name, age, is_active, token, created_on) VALUES ('Alice', 30, 1, '', '')");

        var rows = s.Query<Person>(
            "SELECT id, name, age, is_active AS IsActive FROM people WHERE name = @name",
            new Dictionary<string, object?> { ["@name"] = "Alice" });
        Assert.Single(rows);
    }

    [Fact]
    public void Query_NullableType_NullColumnYieldsNull()
    {
        using var s = Build();
        s.Execute("INSERT INTO people(name, age, is_active, token, created_on, note) VALUES ('Alice', 30, 1, '', '', NULL)");
        var rows = s.Query<Person>("SELECT id, name, age, note FROM people");
        Assert.Null(rows[0].Note);
    }

    [Fact]
    public void QueryDataTable_ReturnsLoadedRows()
    {
        using var s = Build();
        s.Execute("INSERT INTO people(name, age, is_active, token, created_on) VALUES ('Alice', 30, 1, '', '')");
        s.Execute("INSERT INTO people(name, age, is_active, token, created_on) VALUES ('Bob', 25, 0, '', '')");

        var table = s.QueryDataTable("SELECT name, age FROM people ORDER BY age DESC");
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Alice", table.Rows[0]["name"]);
        Assert.Equal("Bob", table.Rows[1]["name"]);
    }

    [Fact]
    public void Execute_LazyOpen_OpensConnectionAutomatically()
    {
        using var s = new SqliteStore();
        s.Setup(":memory:");
        // No explicit Open() — Execute() should open lazily.
        s.Execute("CREATE TABLE x(v INT)");
        Assert.True(s.IsOpen);
    }
}
