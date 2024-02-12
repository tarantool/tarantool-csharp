using ProGaudi.Tarantool.Client;
using ProGaudi.Tarantool.Client.Core;
using ProGaudi.Tarantool.Client.Model;
using ProGaudi.Tarantool.Client.Model.Requests;
using ProGaudi.Tarantool.Client.Model.Responses;

namespace progaudi.tarantool.integration.tests.Pool.Mocks;

public class MockBox : IBox
{
    private bool _alive;
    private readonly BoxInfo _boxInfo;
    private readonly TarantoolNode _node;
    private readonly MockBoxEventRegistrator _eventRegistrator;
    private bool _isConnected = false;
    private BoxInfo _currentBoxInfo;

    public static string GET_CURRENT_INSTANCE_ID = "get_current_instance_id";
    
    public MockBox(bool alive, BoxInfo boxInfo, TarantoolNode node, MockBoxEventRegistrator eventRegistrator)
    {
        _alive = alive;
        _boxInfo = boxInfo;
        _node = node;
        _eventRegistrator = eventRegistrator;
    }

    public void GoDown()
    {
        _alive = false;
        _isConnected = false;
        ConnectionGoesDown?.Invoke(this, new ConnectionWentDownEventArgs(_node));
    }
    
    public void GoUp()
    {
        _alive = true;
    }
    
    public void Dispose()
    {
    }

    public Task Connect()
    {
        if (_alive)
        {
            _isConnected = true;
            _currentBoxInfo = _boxInfo;
        }
        else
        {
            _isConnected = false;
            _currentBoxInfo = null;
        }

        return Task.CompletedTask;
    }

    public bool IsConnected => _isConnected;
    public Metrics Metrics { get; }
    public ISchema Schema { get; }
    public BoxInfo Info => _currentBoxInfo;
    public ISchema GetSchema()
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(GetSchema));
        return null;
    }

    public Task ReloadSchema()
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(ReloadSchema));
        return Task.CompletedTask;
    }

    public Task ReloadBoxInfo()
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(ReloadBoxInfo));
        return Task.CompletedTask;
    }

    public Task Call_1_6(string functionName)
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(Call_1_6));
        return Task.CompletedTask;
    }

    public Task Call_1_6<TTuple>(string functionName, TTuple parameters)
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(Call_1_6));
        return Task.CompletedTask;
    }

    public Task<DataResponse<TResponse[]>> Call_1_6<TResponse>(string functionName)
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(Call_1_6));
        return Task.FromResult<DataResponse<TResponse[]>>(null);
    }

    public Task<DataResponse<TResponse[]>> Call_1_6<TTuple, TResponse>(string functionName, TTuple parameters)
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(Call_1_6));
        return Task.FromResult<DataResponse<TResponse[]>>(null);
    }

    public Task Call(string functionName)
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(Call));
        return Task.CompletedTask;
    }

    public Task Call<TTuple>(string functionName, TTuple parameters)
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(Call));
        return Task.CompletedTask;
    }

    public Task<DataResponse<TResponse[]>> Call<TResponse>(string functionName)
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(Call));
        return Task.FromResult<DataResponse<TResponse[]>>(null);
    }

    public Task<DataResponse<TResponse[]>> Call<TTuple, TResponse>(string functionName, TTuple parameters)
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(Call));
        return Task.FromResult<DataResponse<TResponse[]>>(null);
    }

    public Task<DataResponse<TResponse[]>> Eval<TTuple, TResponse>(string expression, TTuple parameters)
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(Eval));
        return Task.FromResult<DataResponse<TResponse[]>>(null);
    }

    public Task<DataResponse<TResponse[]>> Eval<TResponse>(string expression)
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(Eval));
        return Task.FromResult<DataResponse<TResponse[]>>(null);
    }

    public Task<DataResponse<TResponse[]>> ExecuteSql<TResponse>(string query, params SqlParameter[] parameters)
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(ExecuteSql));
        return Task.FromResult<DataResponse<TResponse[]>>(null);
    }

    public Task<DataResponse> ExecuteSql(string query, params SqlParameter[] parameters)
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(ExecuteSql));
        return Task.FromResult<DataResponse>(null);
    }

    public Task Do<TRequest>(TRequest request) where TRequest : IRequest
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(Do));
        return Task.CompletedTask;
    }

    public Task<DataResponse<TResponse[]>> Do<TRequest, TResponse>(TRequest request) where TRequest : IRequest
    {
        _eventRegistrator.RegisterBoxEvent(_currentBoxInfo.Uuid, nameof(Do));
        return Task.FromResult<DataResponse<TResponse[]>>(null);
    }

    public event EventHandler<ConnectionWentDownEventArgs> ConnectionGoesDown;
}