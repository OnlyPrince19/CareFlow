using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using CareFlow.Data;

namespace CareFlow.Tests;

public static class TestDatabaseFactory
{
    public static (CareFlowDbContext context, SqliteConnection connection) CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<CareFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new CareFlowDbContext(options);

        // Apply migrations and seed default catalog & test patients
        DbInitializer.InitializeAsync(context).GetAwaiter().GetResult();

        return (context, connection);
    }
}
