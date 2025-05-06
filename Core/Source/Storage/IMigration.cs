namespace TaskTrain.Core
{
    public interface IMigration 
    {
        string Name { get; }
        string DowngradeQueryText { get; }
        string UpgradeQueryText { get; }
    }
}
