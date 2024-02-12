namespace ProGaudi.Tarantool.Client.Model
{
    public class ConnectionPoolOptions
    {
        public bool ShuffleNodes { get; set; } = true;

        public int ReconnectIntervalInSeconds { get; set; } = 5;
    }
}