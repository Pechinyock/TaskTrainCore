namespace TaskTrain.Core;

public abstract class StorageUpdaterBase
{
    public event Action<string> OnUpdatedInstalledSucced;
    public event Action<string> OnUpdatedInstalledFailed;

    protected readonly IMigrator _migrator;
    protected readonly string _connectionString;

    protected StorageUpdaterBase(string connectionString
        , IMigrator migrator)
    {
        _migrator = migrator;
        _connectionString = connectionString;
    }

    protected abstract bool CheckConnection();

    /* [TODO]
     * - IMigration could be soted in disk or might be accessible remotly(fancy staff)
     * - implement logic to for this method
     */
    public void UpdateStorage(uint targetVersion)
    {
        const string failedMessagePrefix = "Failed to update storage: ";
        if (!CheckConnection())
        {
            OnUpdatedInstalledFailed?.Invoke($"{failedMessagePrefix} couldn't connect to storage!" +
                $" Check connection string and try again");
            return;
        }

        OnUpdatedInstalledSucced?.Invoke($"Succesufly update storage to version: {targetVersion}");
    }
}
