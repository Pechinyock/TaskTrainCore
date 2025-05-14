namespace TaskTrain.Core
{
    public class SQLMigration 
    {
        public const string MIGRATION_NAMING_PATTERN = @"^[0-9]+-[a-zA-Z0-9_-]+\.sql$";

        public required string Name { get; set; }
        public required string UninstallQueryText { get; set; }
        public required string InstallQueryText { get; set; }
    }
}
