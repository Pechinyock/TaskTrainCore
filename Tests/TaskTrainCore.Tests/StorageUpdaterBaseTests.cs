using TaskTrain.Core;

namespace TaskTrainCore.Tests;

public class StorageUpdaterTests
{
    internal sealed class MigrationPretender : IMigration
    {
        private readonly string _sqlUpText;
        private readonly string _sqlDownText;
        private readonly string _name;

        public string Name => _name;
        public string UninstallQueryText => _sqlDownText;
        public string InstallQueryText => _sqlUpText;

        public MigrationPretender(string name, string sqlText, string sqlDownText)
        {
            _sqlUpText = sqlText;
            _sqlDownText = sqlDownText;
            _name = name;
        }

    }

    internal sealed class StorageUpdaterPretender : StorageUpdaterBase
    {
        private uint _currentVersion;
        private uint _lastVersion;
        private bool _isConnected;

        private IMigration[] _migrations;
        private Action _executeMigrationQueryLogic;

        public StorageUpdaterPretender(string connectionString
            , IMigration[] migrations
            , bool isConnected
            , uint currentVersion
            , uint lastVersion
            , Action executeMigrationQueryLogic = null) : base(connectionString)
        {
            _currentVersion = currentVersion;
            _lastVersion = lastVersion;
            _migrations = migrations;
            _isConnected = isConnected;
            _executeMigrationQueryLogic = executeMigrationQueryLogic;
        }

        protected override bool IsAvailable() => _isConnected;
        protected override uint GetCurrentVersion() => _currentVersion;
        protected override uint GetLastVersion() => _lastVersion;
        protected override IMigration[] GetMigrations() => _migrations;

        protected override void ExecuteMigarionQuery(string queryText)
        {
            _executeMigrationQueryLogic?.Invoke();
        }

    }

    [Fact]
    public void InstnceCreate_Success()
    {
        var exception = Record.Exception(() =>
        {
            var migrations = new IMigration[]
            {
                new MigrationPretender("1", "up 1", "down 1"),
                new MigrationPretender("2", "up 2", "down 2"),
                new MigrationPretender("3", "up 3", "down 3"),
                new MigrationPretender("4", "up 4", "down 4"),
                new MigrationPretender("5", "up 5", "down 5"),
                new MigrationPretender("6", "up 6", "down 6"),
                new MigrationPretender("7", "up 7", "down 7"),
                new MigrationPretender("8", "up 8", "down 8"),
                new MigrationPretender("9", "up 9", "down 9"),
                new MigrationPretender("10", "up 10", "down 10"),
            };

            var storageUpdaterPretender = new StorageUpdaterPretender("fqwer"
                , migrations
                , true
                , 0
                , 10);
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
        var migrations = new IMigration[migrationCount];
        while (migrationCount > iterator)
        {
            migrations[iterator] = new MigrationPretender($"{iterator}", $"up {iterator}", $"down {iterator}");
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

        var storageUpdater = new StorageUpdaterPretender("erdter", migrations, true, currentVersion, 10);

        storageUpdater.OnInstallMigrationSucceed += onMigrationInstalled;

        storageUpdater.UpdateStorage(targetVersion);

        storageUpdater.OnInstallMigrationSucceed -= onMigrationInstalled;
    }

    [Fact]
    internal void StorageIsNotAvailable_Occurs()
    {
        var migrations = new IMigration[10];
        var storageUpdater = new StorageUpdaterPretender("erdter", migrations, false, 0, 10);
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
        var migrations = new IMigration[10];
        var storageUpdater = new StorageUpdaterPretender("erdter", migrations, true, 0, 10);
        bool validationOccurs = false;
        storageUpdater.OnPreValidationFailed += (reason) =>
        {
            validationOccurs = true;
        };

        storageUpdater.UpdateStorage(11);

        Assert.True(validationOccurs);
    }

    [Fact]
    internal void MigrationListEmpyOrNull_Occurs()
    {
        var migrations = new IMigration[10];
        var storageUpdater = new StorageUpdaterPretender("erdter", null, true, 0, 10);
        bool validationOccurs = false;
        storageUpdater.OnPreValidationFailed += (reason) =>
        {
            validationOccurs = true;
        };

        storageUpdater.UpdateStorage(10);
        Assert.True(validationOccurs);

        validationOccurs = false;

        var storageUpdater1 = new StorageUpdaterPretender("erdter", new IMigration[0], true, 0, 10);
        storageUpdater1.OnPreValidationFailed += (reason) =>
        {
            validationOccurs = true;
        };

        storageUpdater1.UpdateStorage(10);
        Assert.True(validationOccurs);
    }

    [Fact]
    internal void CurrentVersionEqualTargetVersion_Occurs()
    {
        var migrations = new IMigration[10];
        var storageUpdater = new StorageUpdaterPretender("erdter", migrations, true, 1, 10);
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
        var migrations = new IMigration[10];
        var storageUpdater = new StorageUpdaterPretender("erdter", migrations, true, 1, 10);
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
        var migrations = new IMigration[]
        {
            new MigrationPretender("1", "up 1", "down 1"),
            new MigrationPretender("2", "up 2", "down 2"),
            new MigrationPretender("3", "up 3", "down 3"),
            new MigrationPretender("4", "up 4", "down 4"),
            new MigrationPretender("5", "up 5", "down 5"),
            null,
            new MigrationPretender("7", "up 7", "down 7"),
            new MigrationPretender("8", "up 8", "down 8"),
            new MigrationPretender("9", "up 9", "down 9"),
            new MigrationPretender("10", "up 10", "down 10"),
        };

        var storageUpdater = new StorageUpdaterPretender("erdter", migrations, true, 0, 10);
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
        var migrations = new IMigration[]
        {
            new MigrationPretender("1", "up 1", "down 1"),
            new MigrationPretender("2", "up 2", "down 2"),
        };

        Action failedInstallation = () => { throw new Exception("failed"); };

        var storageUpdater = new StorageUpdaterPretender("erdter", migrations, true, 0, 10, failedInstallation);
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
        var migrations = new IMigration[migrationCount];
        while (migrationCount > iterator)
        {
            migrations[iterator] = new MigrationPretender($"{iterator}", $"up {iterator}", $"down {iterator}");
            ++iterator;
        }

        var storageUpdater = new StorageUpdaterPretender("erdter", migrations, true, currentVersion, 10);
        storageUpdater.OnUpdateInstalledSucceed += (text, totalInstalled) =>
        {
            Assert.Equal(expectedInstalled, totalInstalled);
        };
        storageUpdater.UpdateStorage(targetVersion);
    }
}
