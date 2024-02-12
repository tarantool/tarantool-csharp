using System;
using System.Threading;
using ProGaudi.Tarantool.Client.Core;
using ProGaudi.Tarantool.Client.Model;

namespace ProGaudi.Tarantool.Client.Pool
{
    internal class ConnectionReconnector
    {
        private Timer _timer;
        private int _disposing;
        private int _attempts;
        private readonly ConnectionPoolOptions _poolOptions;
        private readonly IBox _box;
        private readonly string _key;
        
        public ConnectionReconnector(IBox box, string key, ConnectionPoolOptions poolOptions)
        {
            _box = box;
            _key = key;
            _poolOptions = poolOptions;
        }

        public void SetupReconnection()
        {
            _timer = new Timer(_ => ReconnectAttempt(), null, _poolOptions.ReconnectIntervalInSeconds * 1000, Timeout.Infinite);
        }
        
        public event EventHandler<ConnectionWentUpEventArgs> ConnectionGoesUp;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposing, 1) > 0)
            {
                return;
            }

            Interlocked.Exchange(ref _timer, null)?.Dispose();
        }
        
        private void ReconnectAttempt()
        {
            try
            {
                _attempts++;
                _box.Connect().GetAwaiter().GetResult();
                if (_box.IsConnected)
                {
                    OnConnectionGoesUp(new ConnectionWentUpEventArgs { Box = _box, ConnectionKey = _key });
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                if (_disposing == 0)
                {
                    _timer?.Change(GetReconnectIntervalInMilliseconds(), Timeout.Infinite);
                }
            }
        }

        private void OnConnectionGoesUp(ConnectionWentUpEventArgs e)
        {
            var handler = ConnectionGoesUp;
            handler?.Invoke(this, e);
        }

        private int GetReconnectIntervalInMilliseconds()
        {
            var coef = _attempts <= 10 ? _attempts : 10;
            return _poolOptions.ReconnectIntervalInSeconds * coef * 1000;
        }
    }
}