using System.Reflection;
using System.Text;
using TaskTrain.Core;

namespace TaskTrainCore.Tests;

public class FileSystemMigrationProviderTests
{
    private static string ValidStructureMigrationsPath = FileSystemMigrationPaths.ValidStructureMigrations;
    private static string WithWrongNameFormatMigrationsPath = FileSystemMigrationPaths.WrongNameMigrations;
    private static string WithWrongOrderMigrationsPath = FileSystemMigrationPaths.WrongOrderMigrations;
    private static string WithWrondCountMigrationsPath = FileSystemMigrationPaths.WrongCountMigrations;

    [Fact]
    public void InstanceCreating_Success() 
    {
        var provider = new FileSystemMigrationProvider(ValidStructureMigrationsPath);
    }

    [Fact]
    public void GetMigrationList_NotNull_Success()
    {
        var provider = new FileSystemMigrationProvider(ValidStructureMigrationsPath);
        var migrations = provider.GetMigrations(0, 3);
        Assert.NotNull(migrations);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(0, 2, 2)]
    [InlineData(0, 3, 3)]
    [InlineData(0, 4, 4)]
    [InlineData(1, 2, 1)]
    [InlineData(1, 3, 2)]
    [InlineData(1, 4, 3)]
    [InlineData(2, 3, 1)]
    [InlineData(2, 4, 2)]
    [InlineData(3, 4, 1)]
    public void GetMigrationList_Up_Count_Correct(uint startVersion, uint targetVersion, int expectedCount)
    {
        var provider = new FileSystemMigrationProvider(ValidStructureMigrationsPath);
        var migrations = provider.GetMigrations(startVersion, targetVersion);
        Assert.Equal(expectedCount, migrations.Count());
    }

    [Theory]
    [InlineData(0, 1, new int[] { 1 })]
    [InlineData(1, 2, new int[] { 2 })]
    [InlineData(2, 3, new int[] { 3 })]
    [InlineData(3, 4, new int[] { 4 })]
    [InlineData(0, 2, new int[] { 1, 2 })]
    [InlineData(1, 3, new int[] { 2, 3 })]
    [InlineData(2, 4, new int[] { 3, 4 })]
    [InlineData(0, 3, new int[] { 1, 2, 3 })]
    [InlineData(1, 4, new int[] { 2, 3, 4 })]
    [InlineData(0, 4, new int[] { 1, 2, 3, 4 })]
    public void GetMigrationList_Up_Order_Correct(uint startVersion, uint targetVersion, int[] expectedOrder)
    {
        var provider = new FileSystemMigrationProvider(ValidStructureMigrationsPath);
        var migrations = provider.GetMigrations(startVersion, targetVersion).ToArray();
        var resultOrder = new int[migrations.Length];

        for (int i = 0; i < resultOrder.Length; ++i) 
        {
            var order = ExtractMigrationOrderNumber(migrations[i].Name);
            resultOrder[i] = order;
        }

        Assert.Equal(expectedOrder.Length, resultOrder.Length);

        for (int i = 0; i < expectedOrder.Length; ++i) 
        {
            Assert.Equal(expectedOrder[i], resultOrder[i]);
        }
    }

    [Theory]
    [InlineData(1, 0, 1)]
    [InlineData(2, 0, 2)]
    [InlineData(3, 0, 3)]
    [InlineData(4, 0, 4)]
    [InlineData(2, 1, 1)]
    [InlineData(3, 1, 2)]
    [InlineData(4, 1, 3)]
    [InlineData(3, 2, 1)]
    [InlineData(4, 2, 2)]
    [InlineData(4, 3, 1)]
    public void GetMigrationList_Down_Count_Correct(uint startVersion, uint targetVersion, int expectedCount)
    {
        var provider = new FileSystemMigrationProvider(ValidStructureMigrationsPath);
        var migrations = provider.GetMigrations(startVersion, targetVersion);
        Assert.Equal(expectedCount, migrations.Count());
    }

    [Theory]
    [InlineData(1, 0, new int[] { 1 })]
    [InlineData(2, 1, new int[] { 2 })]
    [InlineData(3, 2, new int[] { 3 })]
    [InlineData(3, 4, new int[] { 4 })]
    [InlineData(2, 0, new int[] { 2, 1 })]
    [InlineData(3, 1, new int[] { 3, 2 })]
    [InlineData(4, 2, new int[] { 4, 3 })]
    [InlineData(3, 0, new int[] { 3, 2, 1 })]
    [InlineData(4, 1, new int[] { 4, 3, 2 })]
    [InlineData(4, 0, new int[] { 4, 3, 2, 1 })]
    public void GetMigrationList_Down_Order_Correct(uint startVersion, uint targetVersion, int[] expectedOrder)
    {
        var provider = new FileSystemMigrationProvider(ValidStructureMigrationsPath);
        var migrations = provider.GetMigrations(startVersion, targetVersion).ToArray();
        var resultOrder = new int[migrations.Length];

        for (int i = 0; i < resultOrder.Length; ++i)
        {
            var order = ExtractMigrationOrderNumber(migrations[i].Name);
            resultOrder[i] = order;
        }

        Assert.Equal(expectedOrder.Length, resultOrder.Length);

        for (int i = 0; i < expectedOrder.Length; ++i)
        {
            Assert.Equal(expectedOrder[i], resultOrder[i]);
        }
    }

    [Fact]
    public void GetMigrationList_Up_NameFormat_Wrong() 
    {
        Assert.Throws<FormatException>(() => 
        {
            var provider = new FileSystemMigrationProvider(WithWrongNameFormatMigrationsPath);
            var list = provider.GetMigrations(0, 4);
        });
    }

    [Fact]
    public void GetMigrationList_Down_NameFormat_Wrong()
    {
        Assert.Throws<FormatException>(() =>
        {
            var provider = new FileSystemMigrationProvider(WithWrongNameFormatMigrationsPath);
            var list = provider.GetMigrations(4, 0);
        });
    }

    [Fact]
    public void GetMigrationList_NameOrder_Wrong()
    {
        Assert.Throws<FormatException>(() =>
        {
            var provider = new FileSystemMigrationProvider(WithWrongOrderMigrationsPath);
            var list = provider.GetMigrations(4, 0);
        });
    }

    [Fact]
    public void GetMigrationList_UpAndDown_CountMismach()
    {
        Assert.Throws<FormatException>(() =>
        {
            var provider = new FileSystemMigrationProvider(WithWrondCountMigrationsPath);
            var list = provider.GetMigrations(4, 0);
        });
    }

    private static int ExtractMigrationOrderNumber(string name)
    {
        var sb = new StringBuilder();

        foreach (var symbol in name)
        {
            if (symbol == '-')
                break;
            if (!Char.IsDigit(symbol))
                return -1;

            sb.Append(symbol);
        }
        var numericPart = sb.ToString();
        if (!int.TryParse(numericPart, out var result))
        {
            return -1;
        }
        return result;
    }
}
