using Moq;
using NUnit.Framework;
using ProGaudi.Tarantool.Client.Model;
using ProGaudi.Tarantool.Client.Pool;
using progaudi.tarantool.integration.tests.Pool.Mocks;

namespace progaudi.tarantool.integration.tests.Pool;

[TestFixture]
public class ConnectionPoolTests
{
    [Test]
    public async Task PoolWithSingleInstance_Call_WorksCorrectly()
    {
        // Arrange
        var nodes = new List<TarantoolNodeMockInfo>
        {
            new() { Alive = true, Uri = "host:12345", Uuid = Guid.NewGuid(), ReadOnly = false }
        };
        var boxEventRegistrator = new MockBoxEventRegistrator();
        var (pool, _, _) = CreateConnectionPoolWithMocks(nodes, boxEventRegistrator);
        var boxEvents = boxEventRegistrator.GetInstanceEvents(nodes[0].Uuid);
        
        // Act
        await pool.Call("do_something", RequestMode.Any);

        // Assert
        Assert.AreEqual(PoolStatus.Connected, pool.Status);
        Assert.AreEqual(1, boxEvents.Count);
        Assert.AreEqual("Call", boxEvents[0]);
    }

    [TestCase(RequestMode.Any, 2, 1, 1)]
    [TestCase(RequestMode.Ro, 2, 0, 2)]
    [TestCase(RequestMode.Rw, 2, 2, 0)]
    [TestCase(RequestMode.PreferRo, 2, 0, 2)]
    [TestCase(RequestMode.PreferRw, 2, 2, 0)]
    public async Task PoolWithRoAndRw_AllRequestModes_WorksCorrectly(RequestMode requestMode, 
        int overallCalls, int expectedRwCalls, int expectedRoCalls)
    {
        // Arrange
        var nodes = new List<TarantoolNodeMockInfo>
        {
            new() { Alive = true, Uri = "host:12345", Uuid = Guid.NewGuid(), ReadOnly = false },
            new() { Alive = true, Uri = "host:12346", Uuid = Guid.NewGuid(), ReadOnly = true }
        };
        var boxEventRegistrator = new MockBoxEventRegistrator();
        var (pool, _, _) = CreateConnectionPoolWithMocks(nodes, boxEventRegistrator);

        var rwEvents = boxEventRegistrator.GetInstanceEvents(nodes[0].Uuid);
        var roEvents = boxEventRegistrator.GetInstanceEvents(nodes[1].Uuid);
        
        // Act 
        foreach (var i in Enumerable.Range(0, overallCalls))
        {
            await pool.Call($"do_something_{i}", requestMode);
        }

        // Assert
        Assert.AreEqual(expectedRwCalls, rwEvents.Count);
        Assert.AreEqual(expectedRoCalls, roEvents.Count);
    }

    [Test]
    public async Task PoolWithRo_RwAndPreferRw_WorksCorrectly()
    {
        // Arrange
        var nodes = new List<TarantoolNodeMockInfo>
        {
            new() { Alive = true, Uri = "host:12345", Uuid = Guid.NewGuid(), ReadOnly = true }
        };
        var boxEventRegistrator = new MockBoxEventRegistrator();
        var (pool, _, _) = CreateConnectionPoolWithMocks(nodes, boxEventRegistrator);
        var events = boxEventRegistrator.GetInstanceEvents(nodes[0].Uuid);
        
        //Act and Assert
        await pool.Call("do_something", RequestMode.PreferRw);
        Assert.AreEqual(1, events.Count);
        
        Assert.ThrowsAsync<NoHealthyNodeException>(
            async () => await pool.Call("do_something", RequestMode.Rw));
        Assert.AreEqual(1, events.Count);
    }
    
    [Test]
    public async Task PoolWithRw_RoAndPreferRo_WorksCorrectly()
    {
        // Arrange
        var nodes = new List<TarantoolNodeMockInfo>
        {
            new() { Alive = true, Uri = "host:12345", Uuid = Guid.NewGuid(), ReadOnly = false }
        };
        var boxEventRegistrator = new MockBoxEventRegistrator();
        var (pool, _, _) = CreateConnectionPoolWithMocks(nodes, boxEventRegistrator);
        var events = boxEventRegistrator.GetInstanceEvents(nodes[0].Uuid);
        
        //Act and Assert
        await pool.Call("do_something", RequestMode.PreferRo);
        Assert.AreEqual(1, events.Count);
        
        Assert.ThrowsAsync<NoHealthyNodeException>(
            async () => await pool.Call("do_something", RequestMode.Ro));
        Assert.AreEqual(1, events.Count);
    }

    [Test]
    public void DeadPool_WorksAsExpected()
    {
        // Arrange
        var nodes = new List<TarantoolNodeMockInfo>
        {
            new() { Alive = false, Uri = "host:12345", Uuid = Guid.NewGuid(), ReadOnly = false },
            new() { Alive = false, Uri = "host:12346", Uuid = Guid.NewGuid(), ReadOnly = true }
        };
        var boxEventRegistrator = new MockBoxEventRegistrator();
        var (pool, _, _) = CreateConnectionPoolWithMocks(nodes, boxEventRegistrator);

        // Act and Assert
        Assert.AreEqual(PoolStatus.Closed, pool.Status);
        Assert.ThrowsAsync<NoHealthyNodeException>(
            async () => await pool.Call("do_something", RequestMode.Any));
    }

    [Test]
    public async Task PoolWithNodeGoesDownAndUp_WorksCorrectly()
    {
        // Arrange
        var nodes = new List<TarantoolNodeMockInfo>
        {
            new() { Alive = true, Uri = "host:12345", Uuid = Guid.NewGuid(), ReadOnly = false }
        };
        var boxEventRegistrator = new MockBoxEventRegistrator();
        var (pool, boxFactory, tarantoolNodes) = CreateConnectionPoolWithMocks(
            nodes, boxEventRegistrator, new ConnectionPoolOptions {ReconnectIntervalInSeconds = 1});
        var events = boxEventRegistrator.GetInstanceEvents(nodes[0].Uuid);
        var box = boxFactory.GetBox(tarantoolNodes[nodes[0].Uri]);

        //Act and Assert
        await pool.Call("do_something", RequestMode.Any);
        Assert.AreEqual(1, events.Count);// it works fine initially
        
        box.GoDown();
        Assert.AreEqual(PoolStatus.Closed, pool.Status);
        Assert.ThrowsAsync<NoHealthyNodeException>(
            async () => await pool.Call("do_something", RequestMode.Any));
        
        box.GoUp();
        Thread.Sleep(1100);//wait for reconnector does its job in background
        
        await pool.Call("do_something", RequestMode.Any);
        Assert.AreEqual(2, events.Count);// it works fine in the end
    }

    private static BoxInfo CreateBoxInfo(Guid uuid, bool readOnly)
    {
        var mockedBoxInfo = new Mock<BoxInfo>();
        mockedBoxInfo.SetupGet(b => b.Uuid).Returns(uuid);
        mockedBoxInfo.SetupGet(b => b.ReadOnly).Returns(readOnly);
        return mockedBoxInfo.Object;
    }
    
    private static (ConnectionPool, MockBoxFactory, Dictionary<string, TarantoolNode>) CreateConnectionPoolWithMocks(
        List<TarantoolNodeMockInfo> nodes,
        MockBoxEventRegistrator boxEventRegistrator,
        ConnectionPoolOptions connectionPoolOptions = null)
    {
        var tarantoolNodes = nodes.ToDictionary(
            x => x.Uri, 
            x => new TarantoolNode(x.Uri));
        
        var mockedNodeSource = new Mock<ITarantoolNodesSource>();
        mockedNodeSource.Setup(s => s.GetNodes())
            .ReturnsAsync(tarantoolNodes.Values.ToList());

        var aliveMapping = nodes.ToDictionary(
            mockInfo => tarantoolNodes[mockInfo.Uri],
            mockInfo => mockInfo.Alive);
        var boxInfoMapping = nodes.ToDictionary(
            mockInfo => tarantoolNodes[mockInfo.Uri],
            mockInfo => CreateBoxInfo(mockInfo.Uuid, mockInfo.ReadOnly));

        var boxFactory = new MockBoxFactory(
            aliveMapping,
            boxInfoMapping,
            boxEventRegistrator);
        var pool = new ConnectionPool(connectionPoolOptions ?? new ConnectionPoolOptions(), 
            mockedNodeSource.Object,
            boxFactory);

        return (pool, boxFactory, tarantoolNodes);
    }

    private class TarantoolNodeMockInfo
    {
        public string Uri { get; init; }
        public Guid Uuid { get; init; }
        public bool ReadOnly { get; init; }
        public bool Alive { get; init; }
    }
}