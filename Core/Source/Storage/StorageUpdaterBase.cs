namespace TaskTrain.Core;

public abstract class StorageUpdaterBase
{
    public event Action<string, uint> OnUpdateInstalledSucceed;
    public event Action<string> OnPreValidationFailed;
    public event Action<string> OnInstallMigrationFailed;
    public event Action<string> OnInstallMigrationSucceed;

    protected readonly string _connectionString;

    protected StorageUpdaterBase(string connectionString)
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

        #region Corner cases 
        /* Data strorage unavailable */
        if (!IsAvailable())
        {
            OnPreValidationFailed?.Invoke($"{failedMessagePrefix} couldn't connect to storage!" +
                " Check connection string and try again");
            return;
        }

        /* taget version higher that last available */
        if (lastVersion < targetVersion)
        {
            OnPreValidationFailed?.Invoke($"{failedMessagePrefix} target version coudn't be higher than last version");
            return;
        }

        /* migrations lista is not empty */
        var mentToInstall = GetMigrations();
        var migrations = mentToInstall?.ToArray();
        if (migrations is null || migrations.Length == 0)
        {
            OnPreValidationFailed?.Invoke($"{failedMessagePrefix} couldn't get migration list");
            return;
        }

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
        #endregion

        var isUpDirection = currentVersion < targetVersion;
        uint totalInstalled = 0;
        var startAt = currentVersion != 0
            ? currentVersion
            : 0;

        while (currentVersion != targetVersion)
        {
            var migrationToInstall = migrations[startAt];
            var queryText = isUpDirection
                ? migrationToInstall.UpgradeQueryText
                : migrationToInstall.DowngradeQueryText;

            try
            {
                var name = migrationToInstall.Name;
                ExecuteMigarionQuery(queryText);
                currentVersion = isUpDirection
                    ? currentVersion + 1
                    : currentVersion - 1;
                OnInstallMigrationSucceed?.Invoke($"{migrationToInstall.Name} installed successfully");
                ++totalInstalled;
            }
            catch (Exception ex) 
            {
                OnInstallMigrationFailed?.Invoke(ex.Message);
                throw;
            }
        }

        OnUpdateInstalledSucceed?.Invoke($"Successfully update storage to version: {targetVersion}"
            , totalInstalled
        );
    }
}
