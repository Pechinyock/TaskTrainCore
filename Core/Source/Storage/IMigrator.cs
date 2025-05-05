namespace TaskTrain.Core
{
    public interface IMigration 
    {
        string GetQueryText();
    }

    public interface IMigrator
    {
        IEnumerable<IMigration> GetMigrations();
        uint GetLastVersion();
        uint GetCurrentVersion();
        void StepForward();
        void StepBackward();
    }
}
