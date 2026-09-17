using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class UnityEventWithDelay<T>
{
    public float delay = 0f;
    public UnityEvent<T> unityEvent = new UnityEvent<T>();

    public void InvokeWithDelay(T value, MonoBehaviour runner)
    {
        runner.StartCoroutine(DelayedInvoke(value));
    }

    private IEnumerator DelayedInvoke(T value)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        unityEvent?.Invoke(value);
    }
}

[Serializable]
public class UnityEventWithDelay
{
    public float delay = 0f;
    public UnityEvent unityEvent = new UnityEvent();

    public void InvokeWithDelay(MonoBehaviour runner)
    {
        runner.StartCoroutine(DelayedInvoke());
    }

    private IEnumerator DelayedInvoke()
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        unityEvent?.Invoke();
    }
}