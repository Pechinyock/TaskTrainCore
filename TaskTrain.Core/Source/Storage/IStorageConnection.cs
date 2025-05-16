namespace TaskTrain.Core;

public interface IStorageConnection
{
    string DataBaseName { get; }
    string ConnectionString { get; }
}
