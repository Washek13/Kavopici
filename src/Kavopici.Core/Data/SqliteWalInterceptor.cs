using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kavopici.Data;

public class SqliteWalInterceptor : DbConnectionInterceptor
{
    private const string Pragmas =
        "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=30000; PRAGMA synchronous=NORMAL;";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ExecutePragmas(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        ExecutePragmas(connection);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private static void ExecutePragmas(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = Pragmas;
        command.ExecuteNonQuery();
    }
}
