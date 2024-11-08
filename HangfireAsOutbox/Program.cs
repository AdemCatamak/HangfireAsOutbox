// See https://aka.ms/new-console-template for more information

using System.Transactions;
using Dapper;
using Hangfire;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

#region Build and Start Test Container

var container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("Password123")
                .Build();

await container.StartAsync();
Console.WriteLine("Test container was started");


var connectionStr = container.GetConnectionString();
Console.WriteLine($"Connection string: {connectionStr}");

#endregion

#region Create Dummy Table

await using (var connection = new SqlConnection(connectionStr))
{
    await connection.ExecuteAsync(sql: "create table DummyTable(Id int identity, Content nvarchar(max))");
}

Console.WriteLine("Table was created");

#endregion

#region Start Hangfire Server

GlobalConfiguration.Configuration
                   .UseSqlServerStorage(connectionStr);
using var server = new BackgroundJobServer();

Console.WriteLine("Hangfire server was started");

#endregion

const string dummyContent1 = "test1";
var dummyId1 = 0;
const string dummyContent2 = "test2";
var dummyId2 = 0;

#region Insert DummyContent1 with Hangfire Task With Transaction Complete

using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    await using (var connection = new SqlConnection(connectionStr))
    {
        dummyId1 = await connection.ExecuteScalarAsync<int>(@"
INSERT INTO [DummyTable] (Content)
OUTPUT INSERTED.Id
VALUES (@Content);", new { Content = dummyContent1 });

        BackgroundJob.Enqueue(() => Console.WriteLine("HF --DummyContent1 was inserted. Id: " + dummyId1));

        scope.Complete();

        Console.WriteLine("Transaction was completed for Id: " + dummyId1);
    }
}

#endregion

#region Insert DummyContent2 with Hangfire Task without Transaction Complete

using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    await using (var connection = new SqlConnection(connectionStr))
    {
        dummyId2 =await connection.ExecuteScalarAsync<int>(@"
INSERT INTO [DummyTable] (Content)
OUTPUT INSERTED.Id
VALUES (@Content);", new { Content = dummyContent2 });

        var now = DateTime.Now;
        BackgroundJob.Enqueue(() => Console.WriteLine("HF -- DummyContent2 was inserted. Id: " + dummyId2));

        // scope.Complete();
    }
}

#endregion

#region Check DummyContent1 and DummyContent2

if (dummyId1 == 0)
{
    throw new Exception("DummyId is 0");
}

await using (var connection = new SqlConnection(connectionStr))
{
    var fetchedContent =
        await connection.QueryFirstOrDefaultAsync<string>(@"SELECT Content FROM [DummyTable] WHERE Id = @id", new { id = dummyId1 });

    if (fetchedContent != dummyContent1)
    {
        throw new Exception("Fetched content is not equal to dummyContent1");
    }
}

#endregion


await using (var connection = new SqlConnection(connectionStr))
{
    var fetchedDummy =
        await connection.QueryFirstOrDefaultAsync<string>(@"SELECT * FROM [DummyTable] WHERE Id = @id", new { id = dummyId2 });

    if (fetchedDummy != null)
    {
        throw new Exception("Fetched content is not null");
    }
}

Console.WriteLine("Thread sleep for 5 seconds");
await Task.Delay(5000);

Console.WriteLine("If you can see more than one message start with 'HF --', it means the test was not successful");
Console.WriteLine("Press any key to exit");
Console.ReadKey();