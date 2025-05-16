namespace TaskTrain.Core.Postgres;

public sealed class PostgreStorageConnection : IStorageConnection
{
    private readonly string _host;
    private readonly string _userName;
    private readonly string _password;
    private readonly ushort _port;
    private readonly string _databaseName;

    public PostgreStorageConnection(string host
        , ushort port
        , string databaseName
        , string userName
        , string password)
    {
        _host = host;
        _userName = userName;
        _password = password;
        _port = port;
        _databaseName = databaseName;
    }

    public string ConnectionString => 
        $"Host={_host};" +
        $"Port={_port};" +
        $"Database={_databaseName};" +
        $"Username={_userName};" +
        $"Password={_password}";

    public string DataBaseName => _databaseName;
}
