using ProGaudi.Tarantool.Client.Model;

namespace ProGaudi.Tarantool.Client.Pool
{
    public interface IBoxFactory
    {
        IBox Create(TarantoolNode node);
    }
}