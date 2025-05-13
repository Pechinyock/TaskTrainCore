using Npgsql;

namespace TaskTrain.Core.Postgres;

public sealed class PostgreStorageUpdater : SQLStorageUpdaterBase
{
    private const string POSTGRES_DEFAULT_DATABASE_NAME = "postgres";

    private readonly NpgsqlDataSource _dataSource;
    private readonly DatabaseMetaInfoProvider _databaseMetaInfo;
    private readonly string _homeDbName;

    public PostgreStorageUpdater(string homeDbName
        , string pgConnectionString
        , string serviceConnectionString) : base(serviceConnectionString)
    {
        var postgresDataSource = NpgsqlDataSource.Create(pgConnectionString);
        _homeDbName = homeDbName;

        _dataSource = NpgsqlDataSource.Create(serviceConnectionString);

        _databaseMetaInfo = new DatabaseMetaInfoProvider(postgresDataSource, _dataSource);
        if (!_databaseMetaInfo.IsDatabaseExists(homeDbName)) 
        {
            _databaseMetaInfo.InitializeDatabase(homeDbName);
        }
    }

    protected override void ExecuteMigarionQuery(string queryText)
    {
        using (var cmd = _dataSource.CreateCommand(queryText)) 
        {
        }
    }

    protected override uint GetCurrentVersion()
    {
        return 0;
    }

    protected override uint GetLastVersion()
    {
        return 2;
    }

    protected override IMigration[] GetMigrations()
    {
        return new IMigration[] {};
    }

    protected override bool IsAvailable()
    {
        try
        {
            using (var cmd = _dataSource.CreateCommand("select 1;"))
            {

            }
            return true;
        }
        catch 
        {
            return false;
        }
    }
}
