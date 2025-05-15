namespace TaskTrain.Core;

public interface IMigrationsPorvider
{
    IEnumerable<SQLMigration> GetMigrations(uint currentVersion, uint targetVersion);

    uint GetLastVersion();
}
