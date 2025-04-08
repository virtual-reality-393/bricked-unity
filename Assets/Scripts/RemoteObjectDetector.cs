using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using PassthroughCameraSamples;
using Meta.XR.EnvironmentDepth;
using Meta.XR;
using Unity.Sentis;
using Unity.XR.CoreUtils;
using Debug = UnityEngine.Debug;

public class RemoteObjectDetector : ObjectDetector
{
    private PythonServer _server;
    private List<DetectedObject> _bricksInternal;
    private Thread _processingTask;
    private bool _playing;

    private byte[] currImgBytes;
    
    Stopwatch sw  = new Stopwatch();
    private RenderTexture _renderTexture;
    private Texture2D _texture;

    async void Start()
    {
        _bricksInternal = new List<DetectedObject>();
        SetWebCam();
        _server = new PythonServer();
        _renderTexture = new RenderTexture(1280, 960, 24);
        _texture = new Texture2D(1280, 960, TextureFormat.RGBA32, false);
        await _server.StartServer();
    }

    // Update is called once per frame
    void Update()
    {
        if (!webcamTexture)
        {
            SetWebCam();
        }

        if (_server.started && !_playing && webcamTexture)
        {
            _playing = true;
            
            _processingTask = new Thread(new ThreadStart(ProcessImages));

            _processingTask.Start();
        }
        
        if(_server.started && _playing && webcamTexture)
        {
            currImgBytes = GetImageFromWebcam();
        }


    }

    void SetWebCam()
    {

        if (webCamTextureManager != null)
        {
            if (webCamTextureManager.WebCamTexture != null)
            {
                webcamTexture = webCamTextureManager.WebCamTexture;
                webcamTexture.Play();
            }
        }
        else
        {
            // This is mostly for local testing
            webcamTexture = new WebCamTexture("QuickCam for Notebooks Deluxe"); // change device index to find correct one

            Debug.Log(webcamTexture.isPlaying);
            Debug.Log(webcamTexture.isReadable);

            webcamTexture.Play();

            Debug.Log(webcamTexture.isPlaying);
            Debug.Log(webcamTexture.isReadable);
        }
    }


    byte[] GetImageFromWebcam()
    {
        Graphics.Blit(webcamTexture, _renderTexture);
        RenderTexture.active = _renderTexture;
        _texture.ReadPixels(new Rect(0, 0, 1280, 960), 0, 0);
        _texture.Apply();

        return _texture.EncodeToTGA();
    }


    async void ProcessImages()
    {
        float scaleX = webcamTexture.width / 640f;
        float scaleY = webcamTexture.height / 640f;
        Stopwatch watch = new Stopwatch();
        while (true)
        {
            try
            {
                while (true)
                {
                   
                    AndroidJNI.AttachCurrentThread();
                    if (!webcamTexture || currImgBytes == null)
                    {
                        Debug.LogError(webcamTexture == null ? "webcamTexture is null" : "webcamTexture is not null");
                        Debug.LogError(currImgBytes == null ? "currImgBytes is null" : "currImgBytes is not null");
                        Thread.Sleep(100);
                        
                        continue;
                    }
                    
                    
                    var camPos = PassthroughCameraUtils.GetCameraPoseInWorld(PassthroughCameraEye.Left);
                    var base64Image = Convert.ToBase64String(currImgBytes);
                   
                    PythonBrick[] pyBrick = await GetBricksFromServer(base64Image);
                    
                    
                    void MainThreadUpdate(PythonBrick[] pythonBricks, Pose pose)
                    {
                        _bricksInternal.Clear();
                        foreach (var brick in pythonBricks)
                        {
                            var bbox = brick.GetBoundingBox();
                    
                            var screenPoint = new Vector2Int((int)(bbox.GetCenter().x * scaleX), (int)((640 - bbox.GetCenter().y) * scaleY));
                            var camRay = PassthroughCameraUtils.ScreenPointToRayInCamera(PassthroughCameraEye.Left, screenPoint);
                            var rayDirectionInWorld = pose.rotation * camRay.direction;
                            if (environmentRaycastManager.Raycast(new Ray(pose.position, rayDirectionInWorld), out EnvironmentRaycastHit hit, 1000f))
                            {
                                _bricksInternal.Add(new DetectedObject(0,brick.color, hit.point));
                    
                                Debug.Log(brick);
                            }
                        }
                    
                        HandleBricksDetected(_bricksInternal);
                    }
                    
                    MainThreadDispatcher.Enqueue(() => MainThreadUpdate(pyBrick,camPos));
                    
                    
                }
                
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            Thread.Sleep(1000);
        }
    }


    T PerformanceTest<T>(Func<T> func)
    {
        Stopwatch sw = new Stopwatch();
        sw.Restart();
        var res = func.Invoke();
        sw.Stop();
        Debug.LogWarning($"Method: {func.Method.Name}, took {sw.ElapsedMilliseconds}, to execute");
        return res;
    }
    
    T PerformanceTest<K,T>(Func<K,T> func, K var1)
    {
        Stopwatch sw = new Stopwatch();
        sw.Restart();
        var res = func.Invoke(var1);
        sw.Stop();
        Debug.LogWarning($"Method: {func.Method.Name}, took {sw.ElapsedMilliseconds}, to execute");
        return res;
    }
    
    void PerformanceTest<T>(Action<T> func, T var1)
    {
        Stopwatch sw = new Stopwatch();
        sw.Restart();
        func.Invoke(var1);
        sw.Stop();
        Debug.LogWarning($"Method: {func.Method.Name}, took {sw.ElapsedMilliseconds}, to execute");
    }
    
    T PerformanceTest<K,V,T>(Func<K,V,T> func, K var1, V var2)
    {
        Stopwatch sw = new Stopwatch();
        sw.Restart();
        var res = func.Invoke(var1,var2);
        sw.Stop();
        Debug.LogWarning($"Method: {func.Method.Name}, took {sw.ElapsedMilliseconds}, to execute");
        return res;
    }

    private async Task<PythonBrick[]> GetBricksFromServer(string base64Image)
    {
        Stopwatch sw = new Stopwatch();
        await _server.SendMessage(base64Image);
        var result = (await _server.ReceiveMessages())[0];
        var bricks = JsonConvert.DeserializeObject<PythonBrick[]>(result);

        return bricks;
    }

    private void OnApplicationQuit()
    {
        if(webcamTexture != null)
        {
            Debug.LogWarning("Destroying Webcam Texture");
            webcamTexture.Stop();
            Destroy(webcamTexture);
        }

        _server.OnApplicationQuit();


        if(_processingTask != null)
        {
            _processingTask.Abort();
        }


    }

    public override List<DetectedObject> GetBricks()
    {
        // var bricks = new List<Brick>();
        //
        // lock (_bricksInternal)
        // {
        //     foreach (var brick in _bricksInternal)
        //     {
        //         bricks.Add(brick);
        //     }
        // }
        //
        // return bricks;

        throw new NotImplementedException(
            "Remote object detection does not currently support manually getting bricks. Please use the event handler instead");
    }
    
}

