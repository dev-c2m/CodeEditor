using System.Collections;
using UnityEngine;

public class CoroutineUtil : MonoBehaviour
{
    private static MonoBehaviour instance;

    [RuntimeInitializeOnLoadMethod]
    private static void Initializer()
    {
        instance = new GameObject($"[{nameof(CoroutineUtil)}]").AddComponent<CoroutineUtil>();
        DontDestroyOnLoad(instance.gameObject);
    }

    public new static Coroutine StartCoroutine(IEnumerator coroutine)
    {
        return instance.StartCoroutine(coroutine);
    }

    public new static void StopCoroutine(Coroutine coroutine)
    {
        instance.StopCoroutine(coroutine);
    }
}
