using System.Linq;
using ProGaudi.Tarantool.Client.Pool;
using Xunit;

namespace ProGaudi.Tarantool.Client.Tests.Pool
{
    public class RoundRobinPoolTests
    {
        [Fact]
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
                Assert.Equal(objs[index], obj);
            }

            Assert.True(pool.IsEmpty());
        }

        [Fact]
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
            Assert.Equal(keys.Length, poolObjects.Length);
            foreach (var index in Enumerable.Range(0, keys.Length))
            {
                Assert.Equal(poolObjects[index], objs[index]);
            }
        }

        [Fact]
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
                Assert.Equal(objs[index], pool.GetByKey(keys[index]));
            }
        }

        [Fact]
        public void AddObjectsByDuplicateKey_WorksCorrectly()
        {
            // Arrange
            var pool = new RoundRobinPool<string, string>();

            // Act
            pool.Add("key", "obj1");
            pool.Add("key", "obj2");

            // Assert
            Assert.Equal("obj2", pool.DeleteByKey("key"));
            Assert.True(pool.IsEmpty());
            Assert.Null(pool.DeleteByKey("key"));
        }

        [Fact]
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
                Assert.Equal(expected[i], pool.GetNext());
            }
        }
    }
}