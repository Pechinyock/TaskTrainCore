namespace TaskTrain.Core;
public abstract class SQLStorageBasicSetupBase
{
    public abstract bool IsConnected();
    public abstract bool IsServiceDatabaseExists(string name);
    public abstract void InitializeBasicSetup(string homeDbName);
}
