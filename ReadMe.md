# Hangfire TransactionScope Compliance Test

This project tests whether Hangfire respects `TransactionScope` by attempting to insert records into a SQL Server database with and without completing the transaction. Hangfire tasks log the insertion actions, and validation checks ensure whether the transactions behaved as expected.

## Project Setup

### Prerequisites
- .NET SDK
- Docker (for the SQL Server container)
- Hangfire, Dapper, and Testcontainers libraries

### Steps

1. **Initialize the Test SQL Server Container**  
   A Docker container for SQL Server is created and started using the `Testcontainers` library. The connection string is retrieved and used for all database operations.

2. **Create a Dummy Table**  
   A `DummyTable` with columns `Id` and `Content` is created within the SQL Server container to store test data.

3. **Start the Hangfire Server**  
   The Hangfire server is started and configured to use the SQL Server storage with the previously defined connection string. Hangfire tasks will be enqueued against this server.

4. **Insert `DummyContent1` with Transaction Complete**
    - A transaction scope is initiated.
    - `DummyContent1` is inserted into `DummyTable`, and its `Id` is retrieved.
    - A Hangfire background job logs the insertion action.
    - `scope.Complete()` is called to commit the transaction.

5. **Insert `DummyContent2` without Completing the Transaction**
    - Another transaction scope is initiated.
    - `DummyContent2` is inserted into `DummyTable`, and its `Id` is retrieved.
    - A Hangfire background job logs the insertion action.
    - The transaction is not completed (no `scope.Complete()`), so this operation should be rolled back.

6. **Verify Transaction Compliance**
    - `DummyContent1` is verified to be present in `DummyTable`, as the transaction was completed.
    - `DummyContent2` should not be found in `DummyTable`, as the transaction was not completed.
    - Hangfire logs are examined to ensure there’s only one successful log for `DummyContent1`.

7. **Test Outcome**
    - If you see more than one message starting with "HF --", it indicates the test failed, meaning Hangfire did not respect `TransactionScope`.
    - Otherwise, if only the log for `DummyContent1` appears, the test is successful, indicating Hangfire complied with `TransactionScope`.

## Run the Project

1. Clone the repository and navigate to the project folder.
2. Run the project with:
   ```bash
   dotnet run
