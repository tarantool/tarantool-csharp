using System.Collections.Concurrent;

namespace progaudi.tarantool.integration.tests.Pool.Mocks;

public class MockBoxEventRegistrator
{
    private readonly ConcurrentDictionary<Guid, List<string>> _registeredEvents;

    public MockBoxEventRegistrator()
    {
        _registeredEvents = new ConcurrentDictionary<Guid, List<string>>();
    }

    public void RegisterBoxEvent(Guid instanceId, string eventName)
    {
        if (_registeredEvents.TryGetValue(instanceId, out var eventList))
        {
            eventList.Add(eventName);
        }
        else
        {
            _registeredEvents.TryAdd(instanceId, new List<string> { eventName });
        }
    }

    public List<string> GetInstanceEvents(Guid instanceId)
    {
        if (_registeredEvents.TryGetValue(instanceId, out var res))
        {
            return res;
        }

        var newList = new List<string>();
        _registeredEvents.TryAdd(instanceId, newList);
        return newList;
    }
}
