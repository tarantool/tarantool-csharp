using NUnit.Framework;
using ProGaudi.Tarantool.Client.Pool;

namespace progaudi.tarantool.integration.tests.Pool;

[TestFixture]
public class RoundRobinPoolTests
{
    [Test]
    public void AddAndDelete_WorksCorrectly()
    {
        // Arrange
        var pool = new RoundRobinPool<string, string>();

        var keys = new string[2] { "key1", "key2" };
        var objs = new string[2] { "obj1", "obj2" };

        // Act and Assert
        foreach (var index in Enumerable.Range(0, keys.Length))
        {
            pool.Add(keys[index], objs[index]);
        }

        foreach (var index in Enumerable.Range(0, keys.Length))
        {
            var obj = pool.DeleteByKey(keys[index]);
            Assert.AreEqual(objs[index], obj);
        }
        
        Assert.True(pool.IsEmpty());
    }
    
    [Test]
    public void AddAndGetAll_WorksCorrectly()
    {
        // Arrange
        var pool = new RoundRobinPool<string, string>();

        var keys = new string[2] { "key1", "key2" };
        var objs = new string[2] { "obj1", "obj2" };

        // Act
        foreach (var index in Enumerable.Range(0, keys.Length))
        {
            pool.Add(keys[index], objs[index]);
        }
        var poolObjects = pool.GetAll();
        
        // Assert
        Assert.AreEqual(keys.Length, poolObjects.Length);
        foreach (var index in Enumerable.Range(0, keys.Length))
        {
            Assert.AreEqual(poolObjects[index], objs[index]);
        }
    }
    
    [Test]
    public void AddAndGetByKey_WorksCorrectly()
    {
        // Arrange
        var pool = new RoundRobinPool<string, string>();

        var keys = new string[2] { "key1", "key2" };
        var objs = new string[2] { "obj1", "obj2" };

        // Act
        foreach (var index in Enumerable.Range(0, keys.Length))
        {
            pool.Add(keys[index], objs[index]);
        }
        
        // Assert
        foreach (var index in Enumerable.Range(0, keys.Length))
        {
            Assert.AreEqual(objs[index], pool.GetByKey(keys[index]));
        }
    }

    [Test]
    public void AddObjectsByDuplicateKey_WorksCorrectly()
    {
        // Arrange
        var pool = new RoundRobinPool<string, string>();
        
        // Act
        pool.Add("key", "obj1");
        pool.Add("key", "obj2");
        
        // Assert
        Assert.AreEqual("obj2", pool.DeleteByKey("key"));
        Assert.True(pool.IsEmpty());
        Assert.IsNull(pool.DeleteByKey("key"));
    }

    [Test]
    public void AddAndGetNext_WorksCorrectly()
    {
        // Arrange
        var pool = new RoundRobinPool<string, string>();

        var keys = new string[2] { "key1", "key2" };
        var objs = new string[2] { "obj1", "obj2" };
        
        foreach (var index in Enumerable.Range(0, keys.Length))
        {
            pool.Add(keys[index], objs[index]);
        }

        // Act and Assert
        var expected = new string[6] { "obj1", "obj2", "obj1", "obj2", "obj1", "obj2" };
        foreach (var i in Enumerable.Range(0, expected.Length))
        {
            Assert.AreEqual(expected[i], pool.GetNext());
        }
    }
}