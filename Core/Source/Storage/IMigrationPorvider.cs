namespace TaskTrain.Core;

public interface IMigrationPorvider
{
    IEnumerable<SQLMigration> GetMigrations(uint currentVersion, uint targetVersion);
}
