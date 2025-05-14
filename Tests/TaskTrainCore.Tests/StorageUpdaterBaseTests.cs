using TaskTrain.Core;

namespace TaskTrainCore.Tests;

public class StorageUpdaterTests
{
    internal sealed class MigrationProviderPretender : IMigrationPorvider
    {
        private readonly IEnumerable<SQLMigration> _migrations;

        public MigrationProviderPretender(IEnumerable<SQLMigration> migrations)
        {
            _migrations = migrations;
        }

        public IEnumerable<SQLMigration> GetMigrations(uint currentVersion, uint targetVersion)
            => _migrations;
    }

    internal sealed class StorageUpdaterPretender : SQLStorageUpdaterBase
    {
        private uint _currentVersion;
        private uint _lastVersion;
        private bool _isConnected;

        private Action _executeMigrationQueryLogic;
        private IMigrationPorvider _migrationPorvider;

        public StorageUpdaterPretender(string connectionString
            , bool isConnected
            , uint currentVersion
            , uint lastVersion
            , IMigrationPorvider migrationPorvider
            , Action executeMigrationQueryLogic = null) : base(connectionString, migrationPorvider)
        {
            _currentVersion = currentVersion;
            _lastVersion = lastVersion;
            _isConnected = isConnected;
            _migrationPorvider = migrationPorvider;
            _executeMigrationQueryLogic = executeMigrationQueryLogic;
            
        }

        protected override bool IsAvailable() => _isConnected;
        protected override uint GetCurrentVersion() => _currentVersion;
        protected override uint GetLastVersion() => _lastVersion;

        protected override void ExecuteMigarionQuery(string queryText)
        {
            _executeMigrationQueryLogic?.Invoke();
        }

        protected override bool IsPrepearedToUpdate() => true;

        protected override void PrepareToUpdate() { }
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
                , 10
                , migrationProvider);
        });
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0, 10, 10, 10)]
    [InlineData(1, 10, 10, 9)]
    [InlineData(2, 10, 10, 8)]
    [InlineData(3, 10, 10, 7)]
    [InlineData(4, 10, 10, 6)]
    [InlineData(5, 10, 10, 5)]
    [InlineData(6, 10, 10, 4)]
    [InlineData(7, 10, 10, 3)]
    [InlineData(8, 10, 10, 2)]
    [InlineData(9, 10, 10, 1)]
    internal void StorageUpdateUp_FromNToMax_Success(uint currentVersion, uint targetVersion, int migrationCount, uint expectedInstalled)
    {
        MigrationInstallationTestLogic(currentVersion, targetVersion, migrationCount, expectedInstalled);
    }

    [Theory]
    [InlineData(10, 0, 10, 10)]
    [InlineData(9, 0, 10, 9)]
    [InlineData(8, 0, 10, 8)]
    [InlineData(7, 0, 10, 7)]
    [InlineData(6, 0, 10, 6)]
    [InlineData(5, 0, 10, 5)]
    [InlineData(4, 0, 10, 4)]
    [InlineData(3, 0, 10, 3)]
    [InlineData(2, 0, 10, 2)]
    [InlineData(1, 0, 10, 1)]
    internal void StorageUpdateDown_FromMaxToN_Success(uint currentVersion, uint targetVersion, int migrationCount, uint expectedInstalled)
    {
        MigrationInstallationTestLogic(currentVersion, targetVersion, migrationCount, expectedInstalled);
    }

    [Theory]
    [InlineData(2, 6, 10, 4)]
    [InlineData(1, 9, 10, 8)]
    [InlineData(0, 100, 100, 100)]
    [InlineData(100, 0, 100, 100)]
    [InlineData(55, 59, 100, 4)]
    [InlineData(0, 999, 1000, 999)]
    internal void StorageUpdateDown_FromNToN_Success(uint currentVersion, uint targetVersion, int migrationCount, uint expectedInstalled)
    {
        MigrationInstallationTestLogic(currentVersion, targetVersion, migrationCount, expectedInstalled);
    }

    [Theory]
    [InlineData(0, 10, 10, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 })]
    [InlineData(3, 5, 10, new int[] { 3, 4 })]
    internal void StroageUpdate_Order_Correct(uint currentVersion, uint targetVersion, int migrationCount, int[] expectedOrder)
    {
        int iterator = 0;
        var migrations = new SQLMigration[migrationCount];
        while (migrationCount > iterator)
        {
            migrations[iterator] = new SQLMigration
            {
                Name = $"{iterator}",
                InstallQueryText = $"up {iterator}",
                UninstallQueryText = $"down {iterator}"
            };
            ++iterator;
        }

        int migrationInstallationIteration = 0;
        Action<string> onMigrationInstalled = (name) =>
        {
            var migrationNumber = int.Parse(name);
            var expectedNow = expectedOrder[migrationInstallationIteration];
            Assert.Equal(expectedNow, migrationNumber);
            ++migrationInstallationIteration;
        };

        var migrationProvider = new MigrationProviderPretender(migrations);

        var storageUpdater = new StorageUpdaterPretender("erdter"
            , true
            , currentVersion
            , 10
            , migrationProvider
        );

        storageUpdater.OnInstallMigrationSucceed += onMigrationInstalled;

        storageUpdater.UpdateStorage(targetVersion);

        storageUpdater.OnInstallMigrationSucceed -= onMigrationInstalled;
    }

    [Fact]
    internal void StorageIsNotAvailable_Occurs()
    {
        var migrations = new SQLMigration[10];
        var migrationProvider = new MigrationProviderPretender(migrations);
        var storageUpdater = new StorageUpdaterPretender("erdter"
            , false
            , 0
            , 10
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
    internal void TargetVersionHigherThanItIsPossible_Occurs()
    {
        var migrations = new SQLMigration[10];
        var migrationProvider = new MigrationProviderPretender(migrations);
        var storageUpdater = new StorageUpdaterPretender("erdter", true, 0, 10, migrationProvider);
        bool validationOccurs = false;
        storageUpdater.OnPreValidationFailed += (reason) =>
        {
            validationOccurs = true;
        };

        storageUpdater.UpdateStorage(11);

        Assert.True(validationOccurs);
    }

    [Fact]
    internal void CurrentVersionEqualTargetVersion_Occurs()
    {
        var migrations = new SQLMigration[10];
        var migrationProvider = new MigrationProviderPretender(migrations);
        var storageUpdater = new StorageUpdaterPretender("erdter", true, 1, 10, migrationProvider);
        bool validationOccurs = false;
        storageUpdater.OnPreValidationFailed += (reason) =>
        {
            validationOccurs = true;
        };

        storageUpdater.UpdateStorage(1);
        Assert.True(validationOccurs);
    }

    [Fact]
    internal void TargetHigherThanLastAvailableVersion_Occurs()
    {
        var migrations = new SQLMigration[10];
        var migrationProvider = new MigrationProviderPretender(migrations);
        var storageUpdater = new StorageUpdaterPretender("erdter", true, 1, 10, migrationProvider);
        bool validationOccurs = false;
        storageUpdater.OnPreValidationFailed += (reason) =>
        {
            validationOccurs = true;
        };

        storageUpdater.UpdateStorage(11);
        Assert.True(validationOccurs);
    }

    [Fact]
    internal void NullMigrationThrow_Occurs()
    {
        var migrations = new SQLMigration[]
        {
            new(){ Name = "1", InstallQueryText = "up 1", UninstallQueryText="down 1" },
            new(){ Name = "2", InstallQueryText = "up 2", UninstallQueryText = "down 2" },
            new(){ Name = "3", InstallQueryText = "up 3", UninstallQueryText = "down 3" },
            new(){ Name = "4", InstallQueryText = "up 4", UninstallQueryText = "down 4" },
            new(){ Name = "5", InstallQueryText = "up 5", UninstallQueryText = "down 5" },
            null,
            new(){ Name = "7", InstallQueryText = "up 7", UninstallQueryText = "down 7" },
            new(){ Name = "8", InstallQueryText = "up 8", UninstallQueryText = "down 8" },
            new(){ Name = "9", InstallQueryText = "up 9", UninstallQueryText = "down 9" },
            new(){ Name = "10", InstallQueryText = "up 10", UninstallQueryText = "down 10" },
        };
        var migrationProvider = new MigrationProviderPretender(migrations);
        var storageUpdater = new StorageUpdaterPretender("erdter", true, 0, 10, migrationProvider);
        bool validationOccurs = false;
        storageUpdater.OnInstallMigrationFailed += (reason) =>
        {
            validationOccurs = true;
        };

        storageUpdater.UpdateStorage(10);
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
        var storageUpdater = new StorageUpdaterPretender("erdter", true, 0, 10, migrationProvider, failedInstallation);
        bool validationOccurs = false;
        storageUpdater.OnInstallMigrationFailed += (reason) =>
        {
            Assert.Equal("failed", reason);
            validationOccurs = true;
        };

        Record.Exception(() => { storageUpdater.UpdateStorage(10); });
        Assert.True(validationOccurs);
    }

    private static void MigrationInstallationTestLogic(uint currentVersion, uint targetVersion, int migrationCount, uint expectedInstalled)
    {
        int iterator = 0;
        var migrations = new SQLMigration[migrationCount];
        while (migrationCount > iterator)
        {
            migrations[iterator] = new SQLMigration
            {
                Name = $"{iterator}",
                InstallQueryText = $"up {iterator}",
                UninstallQueryText = $"down {iterator}"
            };
            ++iterator;
        }
        var migrationProvider = new MigrationProviderPretender(migrations);
        var storageUpdater = new StorageUpdaterPretender("erdter", true, currentVersion, 10, migrationProvider);
        storageUpdater.OnUpdateInstalledSucceed += (text, totalInstalled) =>
        {
            Assert.Equal(expectedInstalled, totalInstalled);
        };
        storageUpdater.UpdateStorage(targetVersion);
    }
}
