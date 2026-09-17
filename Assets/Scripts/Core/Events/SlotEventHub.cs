using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections;
using System.Linq;

public class SlotEventHub : MonoBehaviour
{
    public static SlotEventHub Instance { get; private set; }
    private Dictionary<Type, List<Delegate>> listeners = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Subscribe<T>(Action<T> callback) where T : IGameEvent
    {
        var type = typeof(T);
        if (!listeners.ContainsKey(type))
            listeners[type] = new List<Delegate>();

        listeners[type].Add(callback);
    }

    public void Unsubscribe<T>(Action<T> callback) where T : IGameEvent
    {
        var type = typeof(T);
        if (listeners.ContainsKey(type))
            listeners[type].Remove(callback);
    }

    public void Raise<T>(T gameEvent) where T : SlotEvent
    {
        StartCoroutine(RaiseWithDelay(gameEvent));
    }

    private IEnumerator RaiseWithDelay<T>(T gameEvent) where T : SlotEvent
    {
        if (gameEvent.delaySeconds > 0f)
            yield return new WaitForSeconds(gameEvent.delaySeconds);

        var type = typeof(T);
        if (listeners.TryGetValue(type, out var delegateList))
        {
            foreach (var del in delegateList.Cast<Action<T>>())
            {
                try
                {
                    del?.Invoke(gameEvent);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error invoking {type.Name} listener: {ex.Message}");
                }
            }
        }
    }

}
