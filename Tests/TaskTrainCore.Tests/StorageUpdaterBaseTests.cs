using TaskTrain.Core;

namespace TaskTrainCore.Tests;

public class StorageUpdaterBaseTests
{
    internal sealed class MigrationProviderPretender : IMigrationsPorvider
    {
        private readonly IEnumerable<SQLMigration> _migrations;

        public MigrationProviderPretender(IEnumerable<SQLMigration> migrations)
        {
            _migrations = migrations;
        }

        public uint GetLastVersion()
        {
            return (uint)_migrations.Count();
        }

        public IEnumerable<SQLMigration> GetMigrations(uint currentVersion, uint targetVersion)
            => _migrations;
    }

    internal sealed class StorageUpdaterPretender : SQLStorageUpdaterBase
    {
        private uint _currentVersion;
        private bool _isConnected;

        private Action _executeMigrationQueryLogic;

        public StorageUpdaterPretender(string connectionString
            , bool isConnected
            , uint currentVersion
            , IMigrationsPorvider migrationPorvider
            , Action executeMigrationQueryLogic = null) : base(connectionString, migrationPorvider)
        {
            _currentVersion = currentVersion;
            _isConnected = isConnected;
            _executeMigrationQueryLogic = executeMigrationQueryLogic;
        }

        public override bool IsAvailable() => _isConnected;
        public override uint GetCurrentVersion() => _currentVersion;

        protected override void ExecuteMigarionQuery(string queryText)
        {
            _executeMigrationQueryLogic?.Invoke();
        }

        protected override bool IsPrepearedToUpdate() => true;

        protected override void PrepareToUpdate() { }

        protected override void IncrementVersion()
        {
            ++_currentVersion;
        }

        protected override void DecrementVersion()
        {
            --_currentVersion;
        }
    }

    [Fact]
    public void InstnceCreate_Success()
    {
        var exception = Record.Exception(() =>
        {
            var migrations = new SQLMigration[]
            {
                new(){ Name = "1", InstallQueryText = "up 1", UninstallQueryText="down 1" },
                new(){ Name = "2", InstallQueryText = "up 2", UninstallQueryText = "down 2" },
                new(){ Name = "3", InstallQueryText = "up 3", UninstallQueryText = "down 3" },
                new(){ Name = "4", InstallQueryText = "up 4", UninstallQueryText = "down 4" },
                new(){ Name = "5", InstallQueryText = "up 5", UninstallQueryText = "down 5" },
                new(){ Name = "6", InstallQueryText = "up 6", UninstallQueryText = "down 6" },
                new(){ Name = "7", InstallQueryText = "up 7", UninstallQueryText = "down 7" },
                new(){ Name = "8", InstallQueryText = "up 8", UninstallQueryText = "down 8" },
                new(){ Name = "9", InstallQueryText = "up 9", UninstallQueryText = "down 9" },
                new(){ Name = "10", InstallQueryText = "up 10", UninstallQueryText = "down 10" },
            };
            var migrationProvider = new MigrationProviderPretender(migrations);
            var storageUpdaterPretender = new StorageUpdaterPretender("fqwer"
                , true
                , 0
                , migrationProvider
            );
        });
        Assert.Null(exception);
    }

    [Fact]
    internal void StorageIsNotAvailable_Occurs()
    {
        var migrations = new SQLMigration[10];
        var migrationProvider = new MigrationProviderPretender(migrations);
        var storageUpdater = new StorageUpdaterPretender("erdter"
            , false
            , 0
            , migrationProvider
        );
        bool validationOccurs = false;
        storageUpdater.OnPreValidationFailed += (reason) =>
        {
            validationOccurs = true;
        };

        storageUpdater.UpdateStorage(10);

        Assert.True(validationOccurs);
    }

    [Fact]
    internal void CurrentVersionEqualTargetVersion_Occurs()
    {
        var migrations = new SQLMigration[10];
        var migrationProvider = new MigrationProviderPretender(migrations);
        var storageUpdater = new StorageUpdaterPretender("erdter", true, 1, migrationProvider);
        bool validationOccurs = false;
        storageUpdater.OnPreValidationFailed += (reason) =>
        {
            validationOccurs = true;
        };

        storageUpdater.UpdateStorage(1);
        Assert.True(validationOccurs);
    }

    [Fact]
    internal void InstallMigration_Failed()
    {
        var migrations = new SQLMigration[]
        {
            new(){ Name = "1", InstallQueryText = "up 1", UninstallQueryText="down 1" },
            new(){ Name = "2", InstallQueryText = "up 2", UninstallQueryText = "down 2" },
        };

        Action failedInstallation = () => { throw new Exception("failed"); };
        var migrationProvider = new MigrationProviderPretender(migrations);
        var storageUpdater = new StorageUpdaterPretender("erdter", true, 0, migrationProvider, failedInstallation);
        bool validationOccurs = false;
        storageUpdater.OnInstallMigrationFailed += (reason) =>
        {
            Assert.Equal("failed", reason);
            validationOccurs = true;
        };

        Record.Exception(() => { storageUpdater.UpdateStorage(10); });
        Assert.True(validationOccurs);
    }
}
