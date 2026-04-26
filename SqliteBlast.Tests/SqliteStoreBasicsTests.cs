using System;
using SqliteBlast;
using SqliteBlast.Interfaces;
using Xunit;

namespace SqliteBlast.Tests;

public class SqliteStoreBasicsTests
{
    [Fact]
    public void Setup_NullPath_Throws()
    {
        using var s = new SqliteStore();
        Assert.Throws<ArgumentException>(() => s.Setup(""));
    }

    [Fact]
    public void Open_BeforeSetup_Throws()
    {
        using var s = new SqliteStore();
        Assert.Throws<InvalidOperationException>(() => s.Open());
    }

    [Fact]
    public void IsOpen_DefaultFalse_TrueAfterOpen_FalseAfterClose()
    {
        using var s = new SqliteStore();
        s.Setup(":memory:");
        Assert.False(s.IsOpen);
        s.Open();
        Assert.True(s.IsOpen);
        s.Close();
        Assert.False(s.IsOpen);
    }

    [Fact]
    public void Open_IsIdempotent()
    {
        using var s = new SqliteStore();
        s.Setup(":memory:");
        s.Open();
        s.Open();
        Assert.True(s.IsOpen);
    }

    [Fact]
    public void DatabasePath_ExposesConfiguredValue()
    {
        using var s = new SqliteStore();
        s.Setup(":memory:");
        Assert.Equal(":memory:", s.DatabasePath);
    }

    [Fact]
    public void Factory_Open_ConstructsAndOpens()
    {
        using var s = SqliteBlastFactory.Open(":memory:");
        Assert.True(s.IsOpen);
    }

    [Fact]
    public void Factory_InMemory_OpensWithMemoryDataSource()
    {
        using var s = SqliteBlastFactory.InMemory();
        Assert.True(s.IsOpen);
        Assert.Equal(":memory:", s.DatabasePath);
    }

    [Fact]
    public void Operations_AfterDispose_Throw()
    {
        var s = SqliteBlastFactory.InMemory();
        s.Dispose();
        Assert.Throws<ObjectDisposedException>(() => s.Open());
    }
}
