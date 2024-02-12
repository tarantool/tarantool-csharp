using System.Collections.Generic;
using ProGaudi.Tarantool.Client.Model;

namespace ProGaudi.Tarantool.Client.Pool
{
    public class PooledBoxFactory : IBoxFactory
    {
        private readonly ClientOptions _clientOptions;
        
        public PooledBoxFactory(ClientOptions clientOptions)
        {
            _clientOptions = clientOptions;
        }

        public IBox Create(TarantoolNode node)
        {
            var nodeConnOptions = new ConnectionOptions
            {
                WriteStreamBufferSize = _clientOptions.ConnectionOptions.WriteStreamBufferSize,
                MinRequestsWithThrottle = _clientOptions.ConnectionOptions.MinRequestsWithThrottle,
                MaxRequestsInBatch = _clientOptions.ConnectionOptions.MaxRequestsInBatch,
                WriteThrottlePeriodInMs = _clientOptions.ConnectionOptions.WriteThrottlePeriodInMs,
                ReadStreamBufferSize = _clientOptions.ConnectionOptions.ReadStreamBufferSize,
                WriteNetworkTimeout = _clientOptions.ConnectionOptions.WriteNetworkTimeout,
                ReadNetworkTimeout = _clientOptions.ConnectionOptions.ReadNetworkTimeout,
                PingCheckInterval = _clientOptions.ConnectionOptions.PingCheckInterval,
                PingCheckTimeout = _clientOptions.ConnectionOptions.PingCheckTimeout,
                ReadSchemaOnConnect = true,
                ReadBoxInfoOnConnect = true,
                Nodes = new List<TarantoolNode> { node }
            };

            var nodeBoxOptions =
                new ClientOptions(nodeConnOptions, _clientOptions.LogWriter, _clientOptions.MsgPackContext);
            return new Box(nodeBoxOptions);
        }
    }
}