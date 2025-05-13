namespace TaskTrain.Core;

public abstract class SQLStorageUpdaterBase
{
    public event Action<string, uint> OnUpdateInstalledSucceed;
    public event Action<string> OnPreValidationFailed;
    public event Action<string> OnInstallMigrationFailed;
    public event Action<string> OnInstallMigrationSucceed;

    protected readonly string _connectionString;

    protected SQLStorageUpdaterBase(string connectionString)
    {
        if (String.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        _connectionString = connectionString;
    }

    protected abstract bool IsAvailable();
    protected abstract uint GetCurrentVersion();
    protected abstract uint GetLastVersion();
    protected abstract IMigration[] GetMigrations();
    protected abstract void ExecuteMigarionQuery(string queryText);

    public void UpdateStorage(uint targetVersion)
    {
        const string failedMessagePrefix = "Failed to update storage:";

        var currentVersion = GetCurrentVersion();
        var lastVersion = GetLastVersion();

        /* Data strorage unavailable */
        if (!IsAvailable())
        {
            OnPreValidationFailed?.Invoke($"{failedMessagePrefix} couldn't connect to storage!" +
                " Check connection string and try again");
            return;
        }

        /* taget version higher than last available */
        if (targetVersion > lastVersion)
        {
            OnPreValidationFailed?.Invoke($"{failedMessagePrefix} taget version higher than last available");
            return;
        }

        var allMigrations = GetMigrations();
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

        var iterator = currentVersion;
        var loopThreshold = targetVersion;

        if (iterator >= migrations.Length) 
        {
            iterator = (uint)migrations.Length - 1;
            loopThreshold -= 1;
        }
        
        uint totalInstalled = 0;

        while (iterator != loopThreshold)
        {
            var migrationToInstall = migrations[iterator];
            if (migrationToInstall is null) 
            {
                OnInstallMigrationFailed?.Invoke("Migration list contains 'null' instance");
                return;
            }

            var queryText = isUpDirection
                ? migrationToInstall.InstallQueryText
                : migrationToInstall.UninstallQueryText;

            try
            {
                var name = migrationToInstall.Name;
                ExecuteMigarionQuery(queryText);
                iterator = isUpDirection
                    ? ++iterator
                    : --iterator;

                OnInstallMigrationSucceed?.Invoke($"{migrationToInstall.Name}");
                ++totalInstalled;
            }
            catch (Exception ex)
            {
                OnInstallMigrationFailed?.Invoke(ex.Message);
                throw;
            }
        }

        OnUpdateInstalledSucceed?.Invoke($"{targetVersion}"
            , totalInstalled
        );
    }
}
