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
        public string DowngradeQueryText => _sqlDownText;
        public string UpgradeQueryText => _sqlUpText;

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

        public StorageUpdaterPretender(string connectionString
            , IMigration[] migrations
            , bool isConnected
            , uint currentVersion
            , uint lastVersion) : base(connectionString)
        {
            _currentVersion = currentVersion;
            _lastVersion = lastVersion;
            _migrations = migrations;
            _isConnected = isConnected;
        }

        protected override bool IsAvailable() => _isConnected;
        protected override uint GetCurrentVersion() => _currentVersion;
        protected override uint GetLastVersion() => _lastVersion;
        protected override IMigration[] GetMigrations() => _migrations;

        protected override void ExecuteMigarionQuery(string queryText)
        {

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
    [InlineData(0, 10)]
    [InlineData(1, 9)]
    [InlineData(2, 8)]
    [InlineData(3, 7)]
    [InlineData(4, 6)]
    [InlineData(5, 5)]
    [InlineData(6, 4)]
    [InlineData(7, 3)]
    [InlineData(8, 2)]
    [InlineData(9, 1)]
    internal void MigrationsInstalled_Sucess(uint currentVersion, uint expectedInstalled)
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

        var storageUpdater = new StorageUpdaterPretender("erdter", migrations, true, currentVersion, 10);
        storageUpdater.OnUpdateInstalledSucceed += (text, totalInstalled) => 
        {
            expectedInstalled = (uint)migrations.Length - currentVersion;
            Assert.Equal(expectedInstalled, totalInstalled);
        };
        storageUpdater.UpdateStorage(10);
    }

    [Fact]
    internal void MigrationsInstalled_FromLastToZero_Sucess()
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

        var storageUpdater = new StorageUpdaterPretender("erdter", migrations, true, 9, 10);
        storageUpdater.OnUpdateInstalledSucceed += (text, totalInstalled) =>
        {
            Assert.Equal((uint)migrations.Length, totalInstalled);
        };
        storageUpdater.UpdateStorage(0);
    }
}
