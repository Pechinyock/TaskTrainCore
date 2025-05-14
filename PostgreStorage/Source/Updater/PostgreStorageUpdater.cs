using Npgsql;

namespace TaskTrain.Core.Postgres;

public sealed class PostgreStorageUpdater : SQLStorageUpdaterBase
{
    private const string POSTGRES_DEFAULT_DATABASE_NAME = "postgres";

    private readonly NpgsqlDataSource _dataSource;
    private readonly PostgreStorageBasicSetup _basicSetup;
    private readonly string _homeDbName;

    public PostgreStorageUpdater(string homeDbName
        , string pgConnectionString
        , string serviceConnectionString
        , IMigrationPorvider migrationPorvider) : base(serviceConnectionString, migrationPorvider)
    {
        var postgresDataSource = NpgsqlDataSource.Create(pgConnectionString);

        _homeDbName = homeDbName;
        _dataSource = NpgsqlDataSource.Create(serviceConnectionString);
        _basicSetup = new PostgreStorageBasicSetup(postgresDataSource, _dataSource);
    }

    public PostgreStorageUpdater(IStorageConnection postgresConnection
        , IStorageConnection serviceConnection) : base(serviceConnection)
    {
        if (postgresConnection is null)
            throw new ArgumentNullException(nameof(postgresConnection));

        if (serviceConnection is null)
            throw new ArgumentNullException(nameof(serviceConnection));

        var shouldBePostgres = postgresConnection.DataBaseName;
        if (!shouldBePostgres.Equals(POSTGRES_DEFAULT_DATABASE_NAME, StringComparison.OrdinalIgnoreCase)) 
        {
            throw new ArgumentException($"database name inside postgresConnection has to be:" +
                $" '{POSTGRES_DEFAULT_DATABASE_NAME}'");
        }

        _homeDbName = serviceConnection.DataBaseName;

        var serviceConnectionString = serviceConnection.ConnectionString;
        var serviceDataSource = NpgsqlDataSource.Create(serviceConnectionString);

        var postgresConnectionString = postgresConnection.ConnectionString;
        var postgresDataSource = NpgsqlDataSource.Create(postgresConnectionString);

        _basicSetup = new PostgreStorageBasicSetup(postgresDataSource, serviceDataSource);
        _dataSource = serviceDataSource;
    }

    protected override bool IsPrepearedToUpdate()
    {
        return _basicSetup.IsServiceDatabaseExists(_homeDbName);
    }

    protected override void PrepareToUpdate()
    {
        _basicSetup.InitializeBasicSetup(_homeDbName);
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
