using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = System.Random;

public static class ExtensionMethods
{
    public static T PerformanceTest<T>(this Func<T> func)
    {
        Stopwatch sw = new Stopwatch();
        sw.Restart();
        var res = func.Invoke();
        sw.Stop();
        Debug.LogWarning($"Method: {func.Method.Name}, took {sw.ElapsedMilliseconds} ms, to execute");
        return res;
    }
    
    public static T PerformanceTest<K,T>(this Func<K,T> func, K var1)
    {
        Stopwatch sw = new Stopwatch();
        sw.Restart();
        var res = func.Invoke(var1);
        sw.Stop();
        Debug.LogWarning($"Method: {func.Method.Name}, took {sw.ElapsedMilliseconds} ms, to execute");
        return res;
    }
    
    public static void PerformanceTest<T>(this Action<T> func, T var1)
    {
        Stopwatch sw = new Stopwatch();
        sw.Restart();
        func.Invoke(var1);
        sw.Stop();
        Debug.LogWarning($"Method: {func.Method.Name}, took {sw.ElapsedMilliseconds} ms, to execute");
    }
    
    public static T PerformanceTest<K,V,T>(this Func<K,V,T> func, K var1, V var2)
    {
        Stopwatch sw = new Stopwatch();
        sw.Restart();
        var res = func.Invoke(var1,var2);
        sw.Stop();
        Debug.LogWarning($"Method: {func.Method.Name}, took {sw.ElapsedMilliseconds} ms, to execute");
        return res;
    }

    public static Vector3 RandomWithin(this Bounds bound)
    {
        return new Vector3(
            Mathf.Lerp(bound.min.x, bound.max.x, UnityEngine.Random.value),
            Mathf.Lerp(bound.min.y, bound.max.y, UnityEngine.Random.value),
            Mathf.Lerp(bound.min.z, bound.max.z, UnityEngine.Random.value)
        );
    }

    public static GameObject GetClosest(this IEnumerable<GameObject> objs, Vector3 position)
    {
        GameObject res = null;
        float smallDist = Mathf.Infinity;

        foreach (var obj in objs)
        {
            var dist = Vector3.Distance(obj.transform.position, position);
            if (dist < smallDist)
            {
                smallDist = dist;
                res = obj;
            }
        }

        return res;
    }
    
    public static GameObject GetClosest(this IEnumerable<GameObject> objs, Vector3 position, Func<GameObject,bool> predicate)
    {
        return objs.Where(predicate).GetClosest(position);
    }
}
