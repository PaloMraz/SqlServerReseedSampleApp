using Dapper;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Transactions;

namespace SqlServerReseedSampleApp
{
  class Program
  {
    static async Task Main(string[] args)
    {
      // Localdb connection; the "bigger" SQL Server editions behave exactly the same way.
      const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;";

      const string TestTableName = "__Test";

      // Create Test table.
      using (var connection = new SqlConnection(ConnectionString))
      {
        await connection.OpenAsync();
        await connection.ExecuteAsync($"DROP TABLE IF EXISTS {TestTableName};");
        await connection.ExecuteAsync($"CREATE TABLE {TestTableName}(Id INT IDENTITY NOT NULL PRIMARY KEY);");
      }

      // INSERT row with identity_insert and then reseed without committing the transaction.
      using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
        using (var connection = new SqlConnection(ConnectionString))
        {
          await connection.OpenAsync();

          await connection.ExecuteAsync($"SET IDENTITY_INSERT {TestTableName} ON;");
          await connection.ExecuteAsync($"INSERT INTO {TestTableName} (Id) VALUES (100);");
          await connection.ExecuteAsync($"SET IDENTITY_INSERT {TestTableName} OFF;");
          await connection.ExecuteAsync($"DBCC CHECKIDENT('{TestTableName}', RESEED, 1);");
        }

        // If this is uncommented, the IDENT_CURRENT query below returns 1, if not (i.e. the transaction is rolled back)
        // the ident_current query returns 100!!
        // scope.Complete();
      }

      // Verify RESEED succeeded.
      using (var connection = new SqlConnection(ConnectionString))
      {
        await connection.OpenAsync();
        int seed = await connection.QuerySingleAsync<int>($"SELECT IDENT_CURRENT('{TestTableName}');");
        Console.WriteLine($"Seed = {seed}"); // <-- For rolled back transaction, displays "Seed = 100" instead of "Seed = 1"!
      }

      Console.ReadLine();
    }
  }

}
