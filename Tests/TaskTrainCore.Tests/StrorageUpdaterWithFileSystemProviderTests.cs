using TaskTrain.Core;

namespace TaskTrainCore.Tests;

public class StrorageUpdaterWithFileSystemProviderTests
{
    private static string ValidStructureMigrationsPath = FileSystemMigrationPaths.ValidStructureMigrations;
    private static IMigrationsPorvider MigrationProvider = new FileSystemMigrationProvider(ValidStructureMigrationsPath);

    internal sealed class UpdaterWithFileMigrations : SQLStorageUpdaterBase
    {
        private uint _currentVersion;
        private bool _isAvailable;
        private bool _isPrepearedToUpdate;
        private Action _prepareToUpdate;
        private Action<string> _executeMigrationQuery;

        public UpdaterWithFileMigrations(uint currentVersion
            , bool isAvailable
            , Action prepareToUpdate
            , Action<string> executeMigrationQuery
        ) : base("no connection", MigrationProvider)
        {
            _currentVersion = currentVersion;
            _isAvailable = isAvailable;
            _executeMigrationQuery = executeMigrationQuery;
            _prepareToUpdate = prepareToUpdate;
            _executeMigrationQuery = executeMigrationQuery;
            _isPrepearedToUpdate = _currentVersion != 0;
        }

        protected override void ExecuteMigarionQuery(string queryText)
        {
            _executeMigrationQuery(queryText);
        }

        public override uint GetCurrentVersion()
        {
            return _currentVersion;
        }

        public override bool IsAvailable()
        {
            return _isAvailable;
        }

        protected override bool IsPrepearedToUpdate()
        {
            return _isPrepearedToUpdate;
        }

        protected override void PrepareToUpdate()
        {
            _prepareToUpdate();
        }

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
    public void InstancesCreate_Success()
    {
        var err = Record.Exception(() =>
        {
            var storageUpdated = new UpdaterWithFileMigrations(0, true, () => { }, (sqlText) => { });
        });
        Assert.Null(err);
    }

    [Fact]
    public void UpdatePreporation_Occurs()
    {
        var preporationOcuurs = false;
        var storageUpdated = new UpdaterWithFileMigrations(0
            , true
            , () => { preporationOcuurs = true; }
            , (sqlText) => { }
        );
        storageUpdated.UpdateStorage(4);
        Assert.True(preporationOcuurs);
    }

    [Fact]
    public void UpdatePreporation_NotOccurs()
    {
        var preporationNotOcuurs = true;
        var storageUpdated = new UpdaterWithFileMigrations(1
            , true
            , () => { preporationNotOcuurs = false; }
            , (sqlText) => { }
        );
        storageUpdated.UpdateStorage(4);
        Assert.True(preporationNotOcuurs);
    }

    [Theory]
    [InlineData(0, 4, new string[] { "1 up", "2 up", "3 up", "4 up" })]
    [InlineData(0, 3, new string[] { "1 up", "2 up", "3 up"})]
    [InlineData(0, 2, new string[] { "1 up", "2 up"})]
    [InlineData(0, 1, new string[] { "1 up" })]
    [InlineData(1, 4, new string[] { "2 up", "3 up", "4 up" })]
    [InlineData(1, 3, new string[] { "2 up", "3 up" })]
    [InlineData(1, 2, new string[] { "2 up" })]
    [InlineData(2, 4, new string[] { "3 up", "4 up" })]
    [InlineData(2, 3, new string[] { "3 up" })]
    [InlineData(3, 4, new string[] { "4 up" })]
    [InlineData(4, 1, new string[] { "4 down", "3 down", "2 down", "1 down" })]
    [InlineData(4, 2, new string[] { "4 down", "3 down", "2 down" })]
    [InlineData(4, 3, new string[] { "4 down", "3 down" })]
    [InlineData(3, 1, new string[] { "3 down", "2 down" })]
    [InlineData(3, 2, new string[] { "3 down"})]
    [InlineData(2, 1, new string[] { "2 down"})]

    public void LoadMigrations_Correct(uint startFrom, uint updateTo, string[] expectingReslt)
    {
        var iterrator = 0;

        Action<string> executeMigartionLogic = (sqltext) => 
        {
            var expectedQueryText = expectingReslt[iterrator];
            expectedQueryText.Equals(sqltext, StringComparison.OrdinalIgnoreCase);
            iterrator++;
        };

        var storageUpdated = new UpdaterWithFileMigrations(startFrom
            , true
            , () => { }
            , executeMigartionLogic
        );
        storageUpdated.UpdateStorage(updateTo);
    }
}
