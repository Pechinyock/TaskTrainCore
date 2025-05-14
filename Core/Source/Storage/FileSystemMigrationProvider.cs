using System.Text;
using System.Text.RegularExpressions;

namespace TaskTrain.Core;

public class FileSystemMigrationProvider : IMigrationPorvider
{
    private const string UP_SUBDIR = "up";
    private const string DOWN_SUBDIR = "down";

    private readonly string _targetDirectoryPath;

    public FileSystemMigrationProvider(string targetDirectory)
    {
        if (String.IsNullOrWhiteSpace(targetDirectory))
            throw new ArgumentNullException(nameof(targetDirectory));

        _targetDirectoryPath = targetDirectory;
    }

    public IEnumerable<SQLMigration> GetMigrations(uint currentVersion, uint targetVersion)
    {
        if (!Directory.Exists(_targetDirectoryPath))
            throw new DirectoryNotFoundException($"{_targetDirectoryPath}");

        var upSubDirPath = Path.Combine(_targetDirectoryPath, UP_SUBDIR);
        if (!Directory.Exists(upSubDirPath))
            throw new DirectoryNotFoundException($"{upSubDirPath}");

        var downSubDirPath = Path.Combine(_targetDirectoryPath, DOWN_SUBDIR);
        if (!Directory.Exists(downSubDirPath))
            throw new DirectoryNotFoundException($"{downSubDirPath}");

        var upMigrationsFullPaths = Directory.GetFiles(upSubDirPath);
        var downMigrationsFullPaths = Directory.GetFiles(downSubDirPath);

        if (!IsMigartionsNamesValid(upMigrationsFullPaths, downMigrationsFullPaths))
            throw new FormatException("Migration name has wrong format");

        var versionsDifference = (int)(targetVersion - currentVersion);
        var isUpDirection = versionsDifference > 0;
        var requestedMigrationsCount = Math.Abs(versionsDifference);

        var migrations = new SQLMigration[requestedMigrationsCount];
        var migrationFileCursour = (int)currentVersion;

        if (!isUpDirection || migrationFileCursour >= upMigrationsFullPaths.Length) 
        {
            --migrationFileCursour;
            if(migrationFileCursour <= 0)
                migrationFileCursour = 0;
        }

        for (int i = 0; i < requestedMigrationsCount; ++i)
        {
            var upMigrationsPath = upMigrationsFullPaths[migrationFileCursour];
            var upMigrationName = Path.GetFileName(upMigrationsPath);
            var orderNumber = ExtractMigrationOrderNumber(upMigrationName);

            var upMigrationQuery = File.ReadAllText(upMigrationsFullPaths[i]);
            var downMigrationQuery = File.ReadAllText(downMigrationsFullPaths[i]);

            migrations[i] = new SQLMigration 
            {
                Name = upMigrationName,
                InstallQueryText = upMigrationQuery,
                UninstallQueryText = downMigrationQuery,
            };
            migrationFileCursour = isUpDirection
                ? ++migrationFileCursour
                : --migrationFileCursour;
        }
        return migrations;
    }

    private static bool IsMigartionsNamesValid(string[] upPaths, string[] downPaths)
    {
        if (upPaths.Length != downPaths.Length)
            return false;

        var upNames = new string[upPaths.Length];
        for (int i = 0; i < upNames.Length; ++i)
        {
            upNames[i] = Path.GetFileName(upPaths[i]);
        }
        if (!IsMatchNamePattern(upNames))
            return false;

        var downNames = new string[downPaths.Length];
        for (int i = 0; i < downNames.Length; ++i)
        {
            downNames[i] = Path.GetFileName(downPaths[i]);
        }

        if (!IsMatchNamePattern(downNames))
            return false;

        var sortedUpNames = upNames.OrderBy(x => x).ToArray();
        var sortedDownNames = downNames.OrderBy(x => x).ToArray();

        for (int i = 0; i < sortedUpNames.Length; ++i)
        {
            var orederUpNumber = ExtractMigrationOrderNumber(sortedUpNames[i]);
            if (orederUpNumber == -1)
                return false;

            var orederDownNumber = ExtractMigrationOrderNumber(sortedDownNames[i]);
            if (orederDownNumber == -1)
                return false;

            if (orederUpNumber != orederDownNumber)
                return false;
        }

        return true;
    }

    private static bool IsMatchNamePattern(string[] migrationsNames)
    {
        var pattern = SQLMigration.MIGRATION_NAMING_PATTERN;
        foreach (var name in migrationsNames)
        {
            if (!Regex.IsMatch(name, pattern))
                return false;
        }
        return true;
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
