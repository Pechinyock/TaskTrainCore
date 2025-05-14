using Npgsql;

namespace TaskTrain.Core.Postgres;

internal sealed class PostgreStorageBasicSetup : SQLStorageBasicSetupBase
{
    #region Raw sql

    private static class RawSQL
    {
        public static string IsDatabaseExisis(string dbName) =>
@$"
select exists(
    select datname
    from pg_catalog.pg_database
    where lower(datname) = lower('{dbName}'));
";

        public static string InitializeDatabase(string dbName) =>
@$"
create database {dbName}
with owner
";

        public static string IsRoleExisits(string roleName) =>
@$"
select exists(
    select from pg_catalog.pg_roles
    where rolname = '{roleName}' );
";

        public static string CreateAdminRole() =>
@$"
create role admin
    with
        login
        password 'admin'
        superuser
        createdb
";

        public static string CreateMetaInfoSchema() =>
@$"
create schema if not exists meta AUTHORIZATION admin
";

        public static string CreateServiceHomeDatabase(string dbName) =>

$"create database \"{dbName}\"" +
@" with
    owner=admin
    encoding='UTF8';
";

        public static string CreateVersionsTable() =>
"create table meta.\"versions\"" +
@$"
(
   sigle_row bool primary key default true,
   database_version integer,
   service_version int[],
   constraint sigle_row_constraint check(sigle_row)
);" +
"insert into meta.\"versions\" values (true, 0, array[0, 0, 0])";

    }

    #endregion

    private readonly NpgsqlDataSource _pgSource;
    private readonly NpgsqlDataSource _serviceSource;

    public PostgreStorageBasicSetup(NpgsqlDataSource pgSource, NpgsqlDataSource serviceSource)
    {
        if (pgSource is null)
            throw new ArgumentNullException(nameof(pgSource));

        if (serviceSource is null)
            throw new ArgumentNullException(nameof(serviceSource));

        _pgSource = pgSource;
        _serviceSource = serviceSource;
    }

    public override bool IsServiceDatabaseExists(string databaseName)
    {
        if (String.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentNullException(nameof(databaseName));

        var sqlText = RawSQL.IsDatabaseExisis(databaseName);

        using (var cmd = _pgSource.CreateCommand(sqlText))
        {
            var result = cmd.ExecuteScalar();

            return (bool)result;
        }
    }

    public override void InitializeBasicSetup(string databaseName)
    {
        if (String.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentNullException(nameof(databaseName));

        var isAdminExistsQuery = RawSQL.IsRoleExisits("admin");
        bool isAdminExists = false;

        using (var cmd = _pgSource.CreateCommand(isAdminExistsQuery))
        {
            isAdminExists = (bool)cmd.ExecuteScalar();
        }

        if (!isAdminExists)
        {
            var createAdminRoleQuery = RawSQL.CreateAdminRole();
            using (var cmd = _pgSource.CreateCommand(createAdminRoleQuery))
            {
                cmd.ExecuteNonQuery();
            }
        }

        var createServceDbQuery = RawSQL.CreateServiceHomeDatabase(databaseName);

        using (var cmd = _pgSource.CreateCommand(createServceDbQuery))
        {
            cmd.ExecuteNonQuery();
        }

        var createMetaSchemaQuery = RawSQL.CreateMetaInfoSchema();
        using (var cmd = _serviceSource.CreateCommand(createMetaSchemaQuery))
        {
            cmd.ExecuteNonQuery();
        }

        var createVersionTabeQuery = RawSQL.CreateVersionsTable();
        using (var cmd = _serviceSource.CreateCommand(createVersionTabeQuery))
        {
            cmd.ExecuteNonQuery();
        }
    }
}
