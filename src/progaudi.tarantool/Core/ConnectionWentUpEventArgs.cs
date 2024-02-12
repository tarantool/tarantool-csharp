using System;

namespace ProGaudi.Tarantool.Client.Core
{
    public class ConnectionWentUpEventArgs : EventArgs
    {
        public IBox Box { get; set; }
        public string ConnectionKey { get; set; }
    }
}