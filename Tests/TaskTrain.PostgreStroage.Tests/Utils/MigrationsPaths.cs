using System.Reflection;

namespace PostgreStroage.Tests;

internal class MigrationsPaths
{
    public static readonly string TestsRoot = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

    public static readonly string ScenariosHome = Path.Combine(TestsRoot, "InstallMigration");

    public static string GetScenaio(uint number) => Path.Combine(ScenariosHome, $"Scenario-{number}");

}