using Npgsql;
using TaskTrain.Core.Models;

namespace TaskTrain.Core.Postgres;

public class MetaInfoRepository : IMetaInfoRepository
{
    #region Raw sql
    private static class RawSQL
    {
        public static string MetaInfo => $"select * from meta.versions";

        public static string UpdateDbVersion(uint version) =>
@$"update meta.versions 
    set database_version={version}
    where sigle_row=true;
";

        public static string SetServiceVersion(uint version, uint versionType) =>
$@"
update meta.versions set service_version[{versionType}] = {version}
    where sigle_row is true;
";

    }
    #endregion

    private readonly NpgsqlDataSource _dataSource;

    public MetaInfoRepository(IStorageConnection connection)
    {
        _dataSource = NpgsqlDataSource.Create(connection.ConnectionString);
    }

    public MetaInfoRepository(NpgsqlDataSource source)
    {
        _dataSource = source;
    }

    public int GetDatabaseVersion()
    {
        var sqlQuery = RawSQL.MetaInfo;
        int result = 0;
        using (var cmd = _dataSource.CreateCommand(sqlQuery))
        {
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result = reader.GetInt32(1);
            }
        }
        return result;
    }

    public ServiceVersionModel GetServiceVersion() 
    {
        var sqlQuery = RawSQL.MetaInfo;
        var result = new uint[3];
        using (var cmd = _dataSource.CreateCommand(sqlQuery))
        {
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var rawArray = reader.GetFieldValue<int[]>(2);
                result = Array.ConvertAll(rawArray, val => (uint)val);
            }
        }
        return new ServiceVersionModel() 
        {
            Major = result[0],
            Minor = result[1],
            Patch = result[2]
        };
    }

    public void SetDatabaseVersion(uint value)
    {
        var sqlQuery = RawSQL.UpdateDbVersion(value);
        using (var cmd = _dataSource.CreateCommand(sqlQuery))
        {
            cmd.ExecuteNonQuery();
        }
    }

    public void SetServiceVersion(uint value, ServiceVersionTypeEnum type)
    {
        var sqlQuery = RawSQL.SetServiceVersion(value, (uint)type);
        using (var cmd = _dataSource.CreateCommand(sqlQuery))
        {
            cmd.ExecuteNonQuery();
        }
    }
}
