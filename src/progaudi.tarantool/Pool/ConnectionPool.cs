using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProGaudi.Tarantool.Client.Core;
using ProGaudi.Tarantool.Client.Model;
using ProGaudi.Tarantool.Client.Model.Enums;
using ProGaudi.Tarantool.Client.Model.Requests;
using ProGaudi.Tarantool.Client.Model.Responses;
using ProGaudi.Tarantool.Client.Model.UpdateOperations;

namespace ProGaudi.Tarantool.Client.Pool
{
    public class ConnectionPool : IConnectionPool
    {
        private readonly IPoolStrategy<string, IBox> _roPool;
        private readonly IPoolStrategy<string, IBox> _rwPool;
        private readonly IPoolStrategy<string, IBox> _anyPool;
        private readonly Dictionary<TarantoolNode, IBox> _connectionsByNodes;
        private readonly ConnectionPoolOptions _poolOptions;
        private readonly ITarantoolNodesSource _nodesSource;
        private readonly IBoxFactory _boxFactory;
        
        public ConnectionPool(ConnectionPoolOptions poolOptions, 
            ITarantoolNodesSource nodesSource, IBoxFactory boxFactory)
        {
            _poolOptions = poolOptions;
            _roPool = new RoundRobinPool<string, IBox>();
            _rwPool = new RoundRobinPool<string, IBox>();
            _anyPool = new RoundRobinPool<string, IBox>();
            _connectionsByNodes = new Dictionary<TarantoolNode, IBox>();
            _nodesSource = nodesSource;
            _boxFactory = boxFactory;
            Status = PoolStatus.Unknown;
            
            var somebodyAlive = FillThePools();
            Status = !somebodyAlive ? PoolStatus.Closed : PoolStatus.Connected;
        }
        
        public PoolStatus Status { get; private set; }

        public async Task Call(string functionName, RequestMode mode)
        {
            var box = GetNextConnection(mode);
            await box.Call(functionName);
        }

        public async Task Call<TTuple>(string functionName, TTuple parameters, RequestMode mode)
        {
            var box = GetNextConnection(mode);
            await box.Call(functionName, parameters);
        }

        public async Task<DataResponse<TResponse[]>> Call<TResponse>(string functionName, RequestMode mode)
        {
            var box = GetNextConnection(mode);
            return await box.Call<TResponse>(functionName);
        }

        public async Task<DataResponse<TResponse[]>> Call<TTuple, TResponse>(string functionName, TTuple parameters, RequestMode mode)
        {
            var box = GetNextConnection(mode);
            return await box.Call<TTuple, TResponse>(functionName, parameters);
        }

        public async Task<DataResponse<TResponse[]>> Eval<TTuple, TResponse>(string expression, TTuple parameters, RequestMode mode)
        {
            var box = GetNextConnection(mode);
            return await box.Eval<TTuple, TResponse>(expression, parameters);
        }

        public async Task<DataResponse<TResponse[]>> Eval<TResponse>(string expression, RequestMode mode)
        {
            var box = GetNextConnection(mode);
            return await box.Eval<TResponse>(expression);
        }

        public async Task<DataResponse> ExecuteSql(string query, RequestMode mode, params SqlParameter[] parameters)
        {
            var box = GetNextConnection(mode);
            return await box.ExecuteSql(query, parameters);
        }

        public async Task<DataResponse<TResponse[]>> ExecuteSql<TResponse>(string query, RequestMode mode, params SqlParameter[] parameters)
        {
            var box = GetNextConnection(mode);
            return await box.ExecuteSql<TResponse>(query, parameters);
        }

        public async Task<DataResponse<TTuple[]>> Insert<TTuple>(uint spaceId, TTuple tuple, RequestMode mode = RequestMode.Rw)
        {
            var insertRequest = new InsertRequest<TTuple>(spaceId, tuple);
            return await Do<InsertRequest<TTuple>, TTuple>(insertRequest, mode);
        }

        public async Task<DataResponse<TTuple[]>> Select<TKey, TTuple>(uint spaceId, TKey selectKey, RequestMode mode = RequestMode.Any)
        {
            var selectRequest = new SelectRequest<TKey>(spaceId, Schema.PrimaryIndexId, uint.MaxValue, 0, Iterator.Eq, selectKey);
            return await Do<SelectRequest<TKey>, TTuple>(selectRequest, mode);
        }

        public async Task<TTuple> Get<TKey, TTuple>(uint spaceId, TKey key, RequestMode mode = RequestMode.Any)
        {
            var selectRequest = new SelectRequest<TKey>(spaceId, Schema.PrimaryIndexId, 1, 0, Iterator.Eq, key);
            var response = await Do<SelectRequest<TKey>, TTuple>(selectRequest, mode);
            return response.Data.SingleOrDefault();
        }

        public async Task<DataResponse<TTuple[]>> Replace<TTuple>(uint spaceId, TTuple tuple, RequestMode mode = RequestMode.Rw)
        {
            var replaceRequest = new ReplaceRequest<TTuple>(spaceId, tuple);
            return await Do<InsertReplaceRequest<TTuple>, TTuple>(replaceRequest, mode);
        }

        public async Task<DataResponse<TTuple[]>> Update<TKey, TTuple>(uint spaceId, TKey key, UpdateOperation[] updateOperations,
            RequestMode mode = RequestMode.Rw)
        {
            var updateRequest = new UpdateRequest<TKey>(spaceId, Schema.PrimaryIndexId, key, updateOperations);
            return await Do<UpdateRequest<TKey>, TTuple>(updateRequest, mode);
        }

        public async Task Upsert<TTuple>(uint spaceId, TTuple tuple, UpdateOperation[] updateOperations, RequestMode mode = RequestMode.Rw)
        {
            var upsertRequest = new UpsertRequest<TTuple>(spaceId, tuple, updateOperations);
            await Do(upsertRequest, mode);
        }

        public async Task<DataResponse<TTuple[]>> Delete<TKey, TTuple>(uint spaceId, TKey key, RequestMode mode = RequestMode.Rw)
        {
            var deleteRequest = new DeleteRequest<TKey>(spaceId, Schema.PrimaryIndexId, key);
            return await Do<DeleteRequest<TKey>, TTuple>(deleteRequest, mode);
        }

        public async Task Do<TRequest>(TRequest request, RequestMode mode) where TRequest : IRequest
        {
            var box = GetNextConnection(mode);
            await box.Do(request);
        }

        public async Task<DataResponse<TResponse[]>> Do<TRequest, TResponse>(TRequest request, RequestMode mode) where TRequest : IRequest
        {
            var box = GetNextConnection(mode);
            return await box.Do<TRequest, TResponse>(request);
        }
        
        private bool FillThePools()
        {
            var somebodyAlive = false;

            var nodes = _nodesSource.GetNodes().ConfigureAwait(false).GetAwaiter().GetResult();
            if (_poolOptions.ShuffleNodes)
            {
                var rng = new Random();
                nodes = nodes.OrderBy(a => rng.Next()).ToList();
            }

            Parallel.ForEach(nodes, node =>
            {
                var box = _boxFactory.Create(node);
                
                try
                {
                    box.Connect().ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch
                {
                    // ignored
                }

                _connectionsByNodes[node] = box;
                
                if (box.IsConnected)
                {
                    box.ConnectionGoesDown += ConnectionWentDownHandler;
                    AddConnection(node.Uri.ToString(), box);
                    somebodyAlive = true;
                }
                else
                {
                    AddToReconnectingPool(node.Uri.ToString(), box);
                }
            });

            return somebodyAlive;
        }

        private void AddConnection(string address, IBox box)
        {
            _anyPool.Add(address, box);
            if (box.Info.ReadOnly)
            {
                _roPool.Add(address, box);
            }
            else
            {
                _rwPool.Add(address, box);
            }
        }
        
        private void DeleteConnection(string address)
        {
            _anyPool.DeleteByKey(address);
            _roPool.DeleteByKey(address);
            _rwPool.DeleteByKey(address);
            if (_anyPool.IsEmpty())
            {
                Status = PoolStatus.Closed;
            }
        }

        private IBox GetNextConnection(RequestMode mode)
        {
            IBox next;
            switch (mode)
            {
                case RequestMode.Any:
                    next = _anyPool.GetNext();
                    break;
                case RequestMode.Rw:
                    next = _rwPool.GetNext();
                    break;
                case RequestMode.Ro:
                    next = _roPool.GetNext();
                    break;
                case RequestMode.PreferRw:
                    next = _rwPool.GetNext() ?? _roPool.GetNext();
                    break;
                case RequestMode.PreferRo:
                    next = _roPool.GetNext() ?? _rwPool.GetNext();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            if (next == null)
            {
                throw new NoHealthyNodeException();
            }

            return next;
        }
        
        private void ConnectionWentDownHandler(object sender, ConnectionWentDownEventArgs e)
        {
            DeleteConnection(e.Node.Uri.ToString());
            if (sender is IBox box)
            {
                box.ConnectionGoesDown -= ConnectionWentDownHandler;
                AddToReconnectingPool(e.Node.Uri.ToString(), box);
            }
        }
        
        private void ConnectionWentUpHandler(object sender, ConnectionWentUpEventArgs e)
        {
            if (sender is ConnectionReconnector reconnector)
            {
                RemoveFromReconnectingPool(e.ConnectionKey);
                AddConnection(e.ConnectionKey, e.Box);
                e.Box.ConnectionGoesDown += ConnectionWentDownHandler;
                reconnector.Dispose();
            }
        }

        private readonly Dictionary<string, ConnectionReconnector> _reconnectingPool = new Dictionary<string, ConnectionReconnector>();
        private readonly ReaderWriterLockSlim _reconnectingPoolLock = new ReaderWriterLockSlim();
        
        private void AddToReconnectingPool(string key, IBox box)
        {
            _reconnectingPoolLock.EnterWriteLock();
            try
            {
                if (!_reconnectingPool.ContainsKey(key))
                {
                    var reconnector = new ConnectionReconnector(box, key, _poolOptions);
                    reconnector.ConnectionGoesUp += ConnectionWentUpHandler;
                    reconnector.SetupReconnection();
                    _reconnectingPool[key] = reconnector;
                }
            }
            finally
            {
                _reconnectingPoolLock.ExitWriteLock();
            }
        }

        private void RemoveFromReconnectingPool(string key)
        {
            _reconnectingPoolLock.EnterWriteLock();
            try
            {
                _reconnectingPool.Remove(key);
            }
            finally
            {
                _reconnectingPoolLock.ExitWriteLock();
            }
        }

        public override string ToString()
        {
            return $"Pool of {_anyPool.GetAll().Length} instances. " +
                   $"rw=[{string.Join(" ", _rwPool.GetAll().Select(i => i.Info.Uuid))}] " +
                   $"ro=[{string.Join(" ", _roPool.GetAll().Select(i => i.Info.Uuid))}]";
        }
    }
}