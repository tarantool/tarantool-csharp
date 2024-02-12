using System.Collections.Generic;
using System.Threading.Tasks;
using ProGaudi.Tarantool.Client.Model;

namespace ProGaudi.Tarantool.Client.Pool
{
    public class TarantoolNodesConfigSource : ITarantoolNodesSource
    {
        private readonly ClientOptions _clientOptions;
        
        public TarantoolNodesConfigSource(ClientOptions clientOptions)
        {
            _clientOptions = clientOptions;
        }

        public async Task<IList<TarantoolNode>> GetNodes()
        {
            return await Task.FromResult(_clientOptions.ConnectionOptions.Nodes);
        }
    }
}