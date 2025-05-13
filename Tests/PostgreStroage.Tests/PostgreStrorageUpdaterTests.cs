using Npgsql;
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
        var updater = new PostgreStorageUpdater("user-hub", pgConnectionString, serviceConnectionString);
        updater.UpdateStorage(0);
    }
}
