using TaskTrain.Core;
using TaskTrain.Core.Postgres;

namespace PostgreStroage.Tests;

[Trait("Category", "DebugOnly")]
public class PostgreStrorageUpdaterTests
{
    [Fact]
    public void UpdaterConnect_Success()
    {
        var pgConnectionString = "Host=localhost;Port=7777;Database=postgres;Username=postgres;Password=123456";
        var serviceConnectionString = "Host=localhost;Port=7777;Database=user-hub;Username=admin;Password=admin";
        var migrationProvider = new FileSystemMigrationProvider("NoFolder");
        var updater = new PostgreStorageUpdater("user-hub", pgConnectionString, serviceConnectionString, migrationProvider);
        updater.UpdateStorage(0);
    }

    [Fact]
    public void UpdaterConnectByConnectionClass_Success()
    {
        var postgresConnection = new PostgreStorageConnection(host: "localhost"
            , port: 7777
            , databaseName: "postgres"
            , userName: "postgres"
            , password: "123456"
        );
        var userHubConnection = new PostgreStorageConnection(host: "localhost"
            , port: 7777
            , databaseName: "user-hub"
            , userName: "admin"
            , password: "admin"
        );
        var provider = new FileSystemMigrationProvider(MigrationsPaths.GetScenaio(1));
        var updater = new PostgreStorageUpdater(postgresConnection, userHubConnection, provider);

        var lastVersion = provider.GetLastVersion();
        updater.UpdateStorage(lastVersion);
    }

    [Fact]
    public void IsAvailable_Failed()
    {
        var postgresConnection = new PostgreStorageConnection(host: "localhost"
            , port: 7777
            , databaseName: "postgres"
            , userName: "postgres"
            , password: "123456"
        );

        var userHubConnection = new PostgreStorageConnection(host: "localhost"
            , port: 7777
            , databaseName: "user-hub"
            , userName: "admin"
            , password: "admin"
        );

        var provider = new FileSystemMigrationProvider(MigrationsPaths.GetScenaio(1));
        var updater = new PostgreStorageUpdater(postgresConnection, userHubConnection, provider);

        Action<string> DbNotAvailabe = (error) =>
        {
        };

        var lastVersion = provider.GetLastVersion();

        updater.OnPreValidationFailed += DbNotAvailabe;
        updater.UpdateStorage(lastVersion);
        updater.OnPreValidationFailed -= DbNotAvailabe;
    }

    [Fact]
    public void GetCurrentVersion_Test()
    {
        var userHubConnection = new PostgreStorageConnection(host: "localhost"
            , port: 7777
            , databaseName: "user-hub"
            , userName: "admin"
            , password: "admin"
        );

        var metaInfoProvider = new MetaInfoRepository(userHubConnection);

        var dbVersion = metaInfoProvider.GetDatabaseVersion();
        var serviceVersion = metaInfoProvider.GetServiceVersion();
        metaInfoProvider.SetServiceVersion(1, ServiceVersionTypeEnum.Major);
        metaInfoProvider.SetServiceVersion(2, ServiceVersionTypeEnum.Minor);
        metaInfoProvider.SetServiceVersion(3, ServiceVersionTypeEnum.Patch);
        serviceVersion = metaInfoProvider.GetServiceVersion();
    }
}
