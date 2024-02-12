using System;
using System.Linq;
using ProGaudi.Tarantool.Client.Model;

namespace ProGaudi.Tarantool.Client.Core
{
    public class ConnectionWentDownEventArgs : EventArgs
    {
        public ConnectionWentDownEventArgs(TarantoolNode node)
        {
            Node = node;
        }
        
        public ConnectionWentDownEventArgs(ClientOptions clientOptions)
        {
            Node = clientOptions.ConnectionOptions.Nodes.FirstOrDefault();
        }
        
        public TarantoolNode Node { get; private set; }
    }
}