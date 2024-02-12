using System.Threading.Tasks;
using ProGaudi.Tarantool.Client.Model;
using ProGaudi.Tarantool.Client.Model.Requests;
using ProGaudi.Tarantool.Client.Model.Responses;
using ProGaudi.Tarantool.Client.Model.UpdateOperations;

namespace ProGaudi.Tarantool.Client.Pool
{
    public interface IConnectionPool
    {
        Task Call(string functionName, RequestMode mode);

        Task Call<TTuple>(string functionName, TTuple parameters, RequestMode mode);

        Task<DataResponse<TResponse[]>> Call<TResponse>(string functionName, RequestMode mode);

        Task<DataResponse<TResponse[]>> Call<TTuple, TResponse>(string functionName, TTuple parameters, RequestMode mode);

        Task<DataResponse<TResponse[]>> Eval<TTuple, TResponse>(string expression, TTuple parameters, RequestMode mode);

        Task<DataResponse<TResponse[]>> Eval<TResponse>(string expression, RequestMode mode);

        Task<DataResponse> ExecuteSql(string query, RequestMode mode, params SqlParameter[] parameters);

        Task<DataResponse<TResponse[]>> ExecuteSql<TResponse>(string query, RequestMode mode, params SqlParameter[] parameters);

        Task<DataResponse<TTuple[]>> Insert<TTuple>(uint spaceId, TTuple tuple, RequestMode mode = RequestMode.Rw);

        Task<DataResponse<TTuple[]>> Select<TKey, TTuple>(uint spaceId, TKey selectKey, RequestMode mode = RequestMode.Any);

        Task<TTuple> Get<TKey, TTuple>(uint spaceId, TKey key, RequestMode mode = RequestMode.Any);

        Task<DataResponse<TTuple[]>> Replace<TTuple>(uint spaceId, TTuple tuple, RequestMode mode = RequestMode.Rw);

        Task<DataResponse<TTuple[]>> Update<TKey, TTuple>(uint spaceId, TKey key, UpdateOperation[] updateOperations,
            RequestMode mode = RequestMode.Rw);

        Task Upsert<TTuple>(uint spaceId, TTuple tuple, UpdateOperation[] updateOperations, RequestMode mode = RequestMode.Rw);

        Task<DataResponse<TTuple[]>> Delete<TKey, TTuple>(uint spaceId, TKey key, RequestMode mode = RequestMode.Rw);
        
        Task Do<TRequest>(TRequest request, RequestMode mode) where TRequest : IRequest;

        Task<DataResponse<TResponse[]>> Do<TRequest, TResponse>(TRequest request, RequestMode mode) where TRequest : IRequest;
    }
}