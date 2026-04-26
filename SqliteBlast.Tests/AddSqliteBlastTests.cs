using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SqliteBlast;
using SqliteBlast.Interfaces;
using Xunit;

namespace SqliteBlast.Tests;

public class AddSqliteBlastTests
{
    [Fact]
    public void DirectPath_RegistersWorkingStore()
    {
        var services = new ServiceCollection();
        services.AddSqliteBlast(o => { o.DatabasePath = ":memory:"; });

        using var sp = services.BuildServiceProvider();
        var store = sp.GetRequiredService<ISqliteStore>();

        Assert.True(store.IsOpen);
        store.Execute("CREATE TABLE x(v INT)");
    }

    [Fact]
    public void ResolverPath_TakesPrecedenceOverDirectPath()
    {
        Func<string, string, CancellationToken, Task<string>> resolver = (_, _, _) => Task.FromResult(":memory:");

        var services = new ServiceCollection();
        services.AddSqliteBlast(o =>
        {
            o.DatabasePath    = "/tmp/should-not-be-used.db";
            o.Resolver        = resolver;
            o.ConnectionName  = "scratch";
        });

        using var sp = services.BuildServiceProvider();
        var store = sp.GetRequiredService<ISqliteStore>();

        Assert.Equal(":memory:", store.DatabasePath);
    }

    [Fact]
    public void NeitherPathNorResolver_Throws()
    {
        var services = new ServiceCollection();
        services.AddSqliteBlast(_ => { /* nothing */ });
        using var sp = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => { sp.GetRequiredService<ISqliteStore>(); });
    }
}
