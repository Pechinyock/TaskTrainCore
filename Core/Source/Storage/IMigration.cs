namespace TaskTrain.Core
{
    public interface IMigration 
    {
        string Name { get; }
        string UninstallQueryText { get; }
        string InstallQueryText { get; }
    }
}
