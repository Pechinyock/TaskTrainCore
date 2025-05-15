using System.Text;
using System.Text.RegularExpressions;

namespace TaskTrain.Core;

public class FileSystemMigrationProvider : IMigrationsPorvider
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

    public uint GetLastVersion()
    {
        var upSubDirPath = Path.Combine(_targetDirectoryPath, UP_SUBDIR);
        if (!Directory.Exists(upSubDirPath))
            throw new DirectoryNotFoundException($"{upSubDirPath}");

        var upMigrationsFullPaths = Directory.GetFiles(upSubDirPath);
        return (uint)upMigrationsFullPaths.Length;
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

        ValidateMigrationStructure(upMigrationsFullPaths, downMigrationsFullPaths);

        var versionsDifference = (int)(targetVersion - currentVersion);
        var isUpDirection = versionsDifference > 0;
        var requestedMigrationsCount = Math.Abs(versionsDifference);

        var migrations = new SQLMigration[requestedMigrationsCount];
        var migrationFileCursour = (int)currentVersion;

        if (!isUpDirection || migrationFileCursour >= upMigrationsFullPaths.Length)
        {
            --migrationFileCursour;
            if (migrationFileCursour <= 0)
                migrationFileCursour = 0;
        }

        for (int i = 0; i < requestedMigrationsCount; ++i)
        {
            var upMigrationsPath = upMigrationsFullPaths[migrationFileCursour];
            var upMigrationName = Path.GetFileName(upMigrationsPath);
            var orderNumber = ExtractMigrationOrderNumber(upMigrationName);

            var upMigrationQuery = File.ReadAllText(upMigrationsFullPaths[migrationFileCursour]);
            var downMigrationQuery = File.ReadAllText(downMigrationsFullPaths[migrationFileCursour]);

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

    /* [ISSUE] Comletly shit method
     * 1) try catch should go out
     * 2) should be public to call it before instalation (for example if updater is external programm)
     * 3) provide this method into interafce level
     * 4) might be a good idia to not use execptions here al all
     */
    private static void ValidateMigrationStructure(string[] upPaths, string[] downPaths)
    {
        var issues = new StringBuilder();
        if (upPaths.Length != downPaths.Length)
            throw new FormatException("Up and down migrations count mismatch!");

        var upNames = new string[upPaths.Length];
        for (int i = 0; i < upNames.Length; ++i)
        {
            upNames[i] = Path.GetFileName(upPaths[i]);
        }
        try
        {
            ValidateNames(upNames);
        }
        catch (FormatException ex) 
        {
            issues.Append(ex.Message);
        }

        var downNames = new string[downPaths.Length];
        for (int i = 0; i < downNames.Length; ++i)
        {
            downNames[i] = Path.GetFileName(downPaths[i]);
        }

        try
        {
            ValidateNames(downNames);
        }
        catch (FormatException ex) 
        {
            issues.Append(ex.Message);
        }

        var sortedUpNames = upNames.OrderBy(x => x).ToArray();
        var sortedDownNames = downNames.OrderBy(x => x).ToArray();

        for (int i = 0; i < sortedUpNames.Length; ++i)
        {
            try
            {
                var orederUpNumber = ExtractMigrationOrderNumber(sortedUpNames[i]);
                var orederDownNumber = ExtractMigrationOrderNumber(sortedDownNames[i]);

                if (orederDownNumber <= 0 || orederDownNumber >= int.MaxValue)
                    issues.AppendLine(GetClampErrorMessage(sortedDownNames[i], "down"));

                if (orederUpNumber <= 0 || orederUpNumber >= int.MaxValue)
                    issues.AppendLine(GetClampErrorMessage(sortedUpNames[i], "up"));

                if (orederUpNumber != orederDownNumber)
                {
                    issues.AppendLine($"Migration up: {sortedUpNames[i]}" +
                        $" has different order in name with migration down: {sortedDownNames[i]}");
                }
            }
            catch 
            {
            }
        }

        if (issues.Length > 0)
        {
            throw new FormatException($"Migrations validation failed: {Environment.NewLine}" +
                $" {issues}");
        }
    }

    private static string GetClampErrorMessage(string migrationName, string directionName)
    {
        return $"Migration {directionName}: {migrationName} has wrong format name." +
            $" Numeric part has to be more than 0 and less than: {int.MaxValue}";
    }

    private static void ValidateNames(string[] migrationsNames)
    {
        var pattern = SQLMigration.MIGRATION_NAMING_PATTERN;
        var sb = new StringBuilder();
        foreach (var name in migrationsNames)
        {
            if (!Regex.IsMatch(name, pattern))
            {
                sb.AppendLine($"Migration: '{name}' has wrong name format!");
            }
        }

        if (sb.Length > 0)
        {
            throw new FormatException($"{sb}");
        }
    }

    private static int ExtractMigrationOrderNumber(string name)
    {
        var sb = new StringBuilder();

        foreach (var symbol in name)
        {
            if (symbol == '-')
                break;
            sb.Append(symbol);
        }

        var numericPart = sb.ToString();
        var result = int.Parse(numericPart);
        return result;
    }
}
