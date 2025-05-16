namespace TaskTrain.Core;

public enum ServiceVersionTypeEnum 
{
    Major   = 1,
    Minor   = 2,
    Patch   = 3
}

public interface IMetaInfoRepository
{
    int GetDatabaseVersion();

    ServiceVersionModel GetServiceVersion();

    void SetDatabaseVersion(uint value);

    void SetServiceVersion(uint value, ServiceVersionTypeEnum type);
}
