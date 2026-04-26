using SqliteBlast;
using SqliteBlast.Interfaces;
using Xunit;

namespace SqliteBlast.Tests;

public class SqliteStoreTransactionTests
{
    private static ISqliteStore Build()
    {
        var s = SqliteBlastFactory.InMemory();
        s.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY, v INTEGER)");
        return s;
    }

    [Fact]
    public void DisposeWithoutCommit_RollsBack()
    {
        using var s = Build();
        using (var tx = s.BeginTransaction())
        {
            s.Execute("INSERT INTO t(v) VALUES (1)");
            s.Execute("INSERT INTO t(v) VALUES (2)");
            // No tx.Commit() — dispose should rollback.
        }
        var count = s.ExecuteScalar<long>("SELECT COUNT(*) FROM t");
        Assert.Equal(0, count);
    }

    [Fact]
    public void Commit_PersistsChanges()
    {
        using var s = Build();
        using (var tx = s.BeginTransaction())
        {
            s.Execute("INSERT INTO t(v) VALUES (1)");
            tx.Commit();
        }
        var count = s.ExecuteScalar<long>("SELECT COUNT(*) FROM t");
        Assert.Equal(1, count);
    }

    [Fact]
    public void Commit_AfterDispose_IsNoop()
    {
        using var s = Build();
        var tx = s.BeginTransaction();
        s.Execute("INSERT INTO t(v) VALUES (1)");
        tx.Commit();
        tx.Dispose();      // safe
        tx.Dispose();      // double-dispose safe
        var count = s.ExecuteScalar<long>("SELECT COUNT(*) FROM t");
        Assert.Equal(1, count);
    }
}
