using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class DataLogger
{
    private static readonly ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
    private static readonly string logPath;
    private static Thread logThread;
    private static bool isRunning = false;
    private static Stopwatch _stopwatch = new Stopwatch();

    static DataLogger()
    {
        logPath = Path.Combine(UnityEngine.Application.persistentDataPath, "game.log");
        
        Debug.LogError($"LOGPATH: {logPath}");
        StartLogger();
        AppDomain.CurrentDomain.ProcessExit += (_, __) => StopLogger();
        UnityEngine.Application.quitting += StopLogger;
    }

    public static void Log(string identifier, string message)
    {
        logQueue.Enqueue($"[{_stopwatch.ElapsedMilliseconds}|{identifier}|{Time.frameCount}]{message}");
    }

    private static void StartLogger()
    {
        if (isRunning) return;
        _stopwatch.Start();
        isRunning = true;
        logThread = new Thread(ProcessQueue) { IsBackground = true };
        logThread.Start();
    }

    private static void StopLogger()
    {
        isRunning = false;
        logThread?.Join();
        _stopwatch.Stop();
    }

    private static void ProcessQueue()
    {
        using (var writer = new StreamWriter(logPath, false))
        {
            while (isRunning || !logQueue.IsEmpty)
            {
                if (logQueue.TryDequeue(out string log))
                {
                    writer.WriteLine(log);
                    writer.Flush(); // optional: can buffer to reduce writes
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}