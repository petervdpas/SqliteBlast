using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SqliteBlast;
using SqliteBlast.Interfaces;
using Xunit;

namespace SqliteBlast.Tests;

public class SqliteBlastResolverExtensionsTests
{
    private static Func<string, string, CancellationToken, Task<string>> Recording(
        out List<(string category, string key)> calls,
        Func<string, string, string> respond)
    {
        var capture = new List<(string, string)>();
        calls = capture;
        return (c, k, _) => { capture.Add((c, k)); return Task.FromResult(respond(c, k)); };
    }

    [Fact]
    public async Task SetupAsync_CallsResolverWithDefaultPathKey_AndForwardsValue()
    {
        var resolver = Recording(out var calls, (_, _) => ":memory:");
        using var s = new SqliteStore();

        await s.SetupAsync(resolver, "scratch");

        Assert.Single(calls);
        Assert.Equal(("scratch", "path"), calls[0]);
        Assert.Equal(":memory:", s.DatabasePath);
    }

    [Fact]
    public async Task SetupAsync_HonoursCustomPathKey()
    {
        var resolver = Recording(out var calls, (_, _) => ":memory:");
        using var s = new SqliteStore();

        await s.SetupAsync(resolver, "scratch", pathKey: "file");

        Assert.Equal(("scratch", "file"), calls[0]);
    }

    [Fact]
    public async Task SetupAsync_NullArguments_Throw()
    {
        var resolver = Recording(out _, (_, _) => ":memory:");
        using var s = new SqliteStore();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ((ISqliteStore)null!).SetupAsync(resolver, "x"));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            s.SetupAsync(null!, "x"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            s.SetupAsync(resolver, ""));
    }

    [Fact]
    public async Task SetupAsync_FollowedByOpen_ConnectsToResolvedPath()
    {
        var resolver = Recording(out _, (_, _) => ":memory:");
        using var s = new SqliteStore();

        await s.SetupAsync(resolver, "scratch");
        s.Open();

        Assert.True(s.IsOpen);
        s.Execute("CREATE TABLE x(v INT)");
        Assert.Equal(0L, s.ExecuteScalar<long>("SELECT COUNT(*) FROM x"));
    }
}
