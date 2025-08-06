
using System;
using System.Collections.Generic;

public class Observer : Singleton<Observer>
{
    private readonly Dictionary<string, HashSet<Action<object>>> _observes = new Dictionary<string, HashSet<Action<object>>>();

    public void AddObserver(string topicName, Action<object> observerCallback)
    {
        var observerList = GetObserverList(topicName);
        observerList.Add(observerCallback);
    }

    public void RemoveObserver(string topicName, Action<object> observerCallback)
    {
        var observerList = GetObserverList(topicName);
        observerList.Remove(observerCallback);
    }

    public void Notify(string topicName, object data)
    {
        var observerList = GetObserverList(topicName);

        foreach (var observer in observerList)
        {
            observer(data);
        }
    }

    public void Notify(string topicName)
    {
        Notify(topicName, null);
    }

    private HashSet<Action<object>> GetObserverList(string topicName)
    {
        if (!_observes.ContainsKey(topicName))
        {
            _observes.Add(topicName, new HashSet<Action<object>>());
        }
        return _observes[topicName];
    }

}
