using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProGaudi.Tarantool.Client.Model;

namespace ProGaudi.Tarantool.Client.Pool
{
    public interface ITarantoolNodesSource
    {
        Task<IList<TarantoolNode>> GetNodes();
    }
}