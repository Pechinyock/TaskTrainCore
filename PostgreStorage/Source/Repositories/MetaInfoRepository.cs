using Npgsql;

namespace TaskTrain.Core.Postgres;

public class MetaInfoRepository
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
        var fromDb = new int[3];
        using (var cmd = _dataSource.CreateCommand(sqlQuery))
        {
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                fromDb = reader.GetFieldValue<int[]>(2);
            }
        }
        return new ServiceVersionModel() 
        {
            Major = fromDb[0],
            Minor = fromDb[1],
            Patch = fromDb[2]
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
}
