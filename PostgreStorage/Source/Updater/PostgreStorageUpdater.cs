using Npgsql;

namespace TaskTrain.Core.Postgres;

public sealed class PostgreStorageUpdater : StorageUpdaterBase
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgreStorageUpdater(string connectionString) : base(connectionString)
    {
        _dataSource = NpgsqlDataSource.Create(connectionString);
    }

    protected override void ExecuteMigarionQuery(string queryText)
    {
        using (var cmd = _dataSource.CreateCommand("select 1;")) 
        {

        }
    }

    protected override uint GetCurrentVersion()
    {
        throw new NotImplementedException();
    }

    protected override uint GetLastVersion()
    {
        throw new NotImplementedException();
    }

    protected override IMigration[] GetMigrations()
    {
        throw new NotImplementedException();
    }

    protected override bool IsAvailable()
    {
        throw new NotImplementedException();
    }
}
