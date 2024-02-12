using System;

namespace ProGaudi.Tarantool.Client.Core
{
    public class ConnectionTimeoutThresholdReachedEventArgs : EventArgs
    {
        public uint TimeoutCount { get; set; }
    }
}