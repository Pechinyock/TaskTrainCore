using System.Reflection;

namespace TaskTrainCore.Tests;

internal static class FileSystemMigrationPaths
{
    public static readonly string TestsRoot = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

    public static readonly string ValidStructureMigrations = Path.Combine(TestsRoot, "SuccessCase");

    public static readonly string WrongNameMigrations = Path.Combine(TestsRoot, "WrongFormatCase");

    public static readonly string WrongOrderMigrations = Path.Combine(TestsRoot, "OrderMismatchCase");

    public static readonly string WrongCountMigrations = Path.Combine(TestsRoot, "CountMissmatchCase");
}
