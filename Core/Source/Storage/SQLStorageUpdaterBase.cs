namespace TaskTrain.Core;

public abstract class SQLStorageUpdaterBase
{
    public event Action<string, uint> OnUpdateInstalledSucceed;
    public event Action<string> OnPreValidationFailed;
    public event Action<string> OnInstallMigrationFailed;
    public event Action<string> OnInstallMigrationSucceed;

    protected readonly string _connectionString;
    protected readonly IMigrationsPorvider _migrationsProvider;

    protected SQLStorageUpdaterBase(string connectionString, IMigrationsPorvider migrationPorvider)
    {
        if (String.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        if(migrationPorvider is null)
            throw new ArgumentNullException(nameof(migrationPorvider));

        _connectionString = connectionString;
        _migrationsProvider = migrationPorvider;
    }

    protected SQLStorageUpdaterBase(IStorageConnection connection, IMigrationsPorvider migrationPorvider) 
    {
        if(connection is null)
            throw new ArgumentNullException(nameof(connection));

        if(String.IsNullOrEmpty(connection.ConnectionString))
            throw new ArgumentNullException(nameof(connection.ConnectionString));

        _connectionString = connection.ConnectionString;
        _migrationsProvider = migrationPorvider;
    }

    public abstract bool IsAvailable();
    public abstract uint GetCurrentVersion();

    protected abstract void ExecuteMigarionQuery(string queryText);
    protected abstract bool IsPrepearedToUpdate();
    protected abstract void PrepareToUpdate();
    protected abstract void IncrementVersion();
    protected abstract void DecrementVersion();

    public void UpdateStorage(uint targetVersion)
    {
        const string failedMessagePrefix = "Failed to update storage:";
        /* Data strorage unavailable */
        if (!IsAvailable())
        {
            OnPreValidationFailed?.Invoke($"{failedMessagePrefix} couldn't connect to storage!" +
                " Check connection string and try again");
            return;
        }

        if (!IsPrepearedToUpdate())
            PrepareToUpdate();

        var currentVersion = GetCurrentVersion();

        var allMigrations = _migrationsProvider.GetMigrations(currentVersion, targetVersion);
        var migrations = allMigrations?.ToArray();

        /* migrations list is not empty */
        if (migrations is null || migrations.Length == 0)
        {
            OnPreValidationFailed?.Invoke($"{failedMessagePrefix} couldn't get migration list");
            return;
        }

        /* current version migrations count missmach */
        if (migrations.Length < currentVersion)
        {
            OnPreValidationFailed?.Invoke($"{failedMessagePrefix} migraions count couldn't be lower than current version" +
                " are you missing some migrations?");
            return;
        }

        /* already at target version */
        if (currentVersion == targetVersion)
        {
            OnPreValidationFailed?.Invoke($"{failedMessagePrefix} Datastorage already at version: {targetVersion}");
            return;
        }

        var isUpDirection = currentVersion < targetVersion;

        var migrationProcedType = isUpDirection
            ? "install"
            : "uninstall";

        uint totalProceed = 0;
        foreach (var migration in migrations) 
        {
            var queryText = isUpDirection
                ? migration.InstallQueryText
                : migration.UninstallQueryText;

            try
            {
                ExecuteMigarionQuery(queryText);
                OnInstallMigrationSucceed?.Invoke($"{migrationProcedType}: {migration.Name}");

                if (isUpDirection)
                    IncrementVersion();
                else 
                    DecrementVersion();

                ++totalProceed;
            }
            catch (Exception ex)
            {
                OnInstallMigrationFailed?.Invoke(ex.Message);
                throw;
            }
        }

        OnUpdateInstalledSucceed?.Invoke($"{targetVersion}"
            , totalProceed
        );
    }
}
