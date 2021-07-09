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
      const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;";
      const string TestTableName = "__Test";

      // Create Test table.
      using (var connection = new SqlConnection(ConnectionString))
      {
        await connection.OpenAsync();
        await connection.ExecuteAsync($"drop table if exists {TestTableName};");
        await connection.ExecuteAsync($"create table {TestTableName}(Id int identity not null primary key);");
      }

      // INSERT row with identity_insert and then reseed without committing the transaction.
      using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
        using (var connection = new SqlConnection(ConnectionString))
        {
          await connection.OpenAsync();

          await connection.ExecuteAsync($"set identity_insert {TestTableName} on;");
          await connection.ExecuteAsync($"insert into {TestTableName} (Id) values (100);");
          await connection.ExecuteAsync($"set identity_insert {TestTableName} off;");
          await connection.ExecuteAsync($"dbcc checkident ('{TestTableName}', reseed, 1);");
        }

        // If this is uncommented, the ident_current query below returns 1, if not (i.e. the transaction is rolled back)
        // the ident_current query returns 100!!
        // scope.Complete();
      }

      // Verify RESEED succeeded.
      using (var connection = new SqlConnection(ConnectionString))
      {
        await connection.OpenAsync();
        int seed = await connection.QuerySingleAsync<int>($"select ident_current('{TestTableName}');");
        Console.WriteLine($"Seed = {seed}"); // <-- For rolled back transaction, displays "Seed = 100" instead of "Seed = 1"!
      }

      Console.ReadLine();
    }
  }

}
