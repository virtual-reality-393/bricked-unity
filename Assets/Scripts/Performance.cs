using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Performance : MonoBehaviour
{
    private float _count;

    public ObjectDetector detector;

    public Stopwatch watch;

    private IEnumerator FPSCounter()
    {
        GUI.depth = 2;
        while (true)
        {
            _count = 1f / Time.unscaledDeltaTime;
            Debug.LogWarning($"FPS: {_count}");
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Start()
    {
        // StartCoroutine(FPSCounter());
        detector.OnObjectsDetected += OnObjectsDetected;
        watch = new Stopwatch();
        
        watch.Start();
    }

    private void OnObjectsDetected(object sender, ObjectDetectedEventArgs e)
    {
        var timeTaken = watch.ElapsedMilliseconds;
        
        if (timeTaken <= 0) return;
        
        Debug.LogWarning($"Object detection time taken: {timeTaken}ms");

        watch.Restart();
    }

}
