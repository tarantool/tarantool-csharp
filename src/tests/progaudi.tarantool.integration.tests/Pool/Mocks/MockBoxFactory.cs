using ProGaudi.Tarantool.Client;
using ProGaudi.Tarantool.Client.Model;
using ProGaudi.Tarantool.Client.Pool;

namespace progaudi.tarantool.integration.tests.Pool.Mocks;

public class MockBoxFactory : IBoxFactory
{
    private readonly Dictionary<TarantoolNode, bool> _aliveMapping;
    private readonly Dictionary<TarantoolNode, BoxInfo> _boxInfoMapping;
    private readonly Dictionary<TarantoolNode, MockBox> _boxMapping;
    private readonly MockBoxEventRegistrator _boxEventRegistrator;

    public MockBoxFactory(Dictionary<TarantoolNode, bool> aliveMapping, 
        Dictionary<TarantoolNode, BoxInfo> boxInfoMapping,
        MockBoxEventRegistrator boxEventRegistrator)
    {
        _aliveMapping = aliveMapping;
        _boxInfoMapping = boxInfoMapping;
        _boxEventRegistrator = boxEventRegistrator;
        _boxMapping = new Dictionary<TarantoolNode, MockBox>();
    }

    public IBox Create(TarantoolNode node)
    {
        var box = new MockBox(_aliveMapping[node], _boxInfoMapping[node], node, _boxEventRegistrator);
        _boxMapping[node] = box;
        return box;
    }

    public MockBox GetBox(TarantoolNode node)
    {
        return _boxMapping.TryGetValue(node, out var box) ? box : null;
    }
}