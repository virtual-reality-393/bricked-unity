using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using PassthroughCameraSamples;
using Meta.XR.EnvironmentDepth;
using Meta.XR;
using Unity.Sentis;

public class RemoteObjectDetector : ObjectDetector
{
    private PythonServer _server;
    private List<Brick> _bricksInternal;
    private Task _processingTask;
    private bool _playing;

    async void Start()
    {
        _bricksInternal = new List<Brick>();
        SetWebCam();
        _server = new PythonServer();
        await _server.StartServer();
    }

    // Update is called once per frame
    void Update()
    {
        if (webcamTexture == null)
        {
            SetWebCam();
        }

        if (_server.started && !_playing && webcamTexture != null)
        {
            _playing = true;

            _processingTask = ProcessImages();

            _processingTask.Start();
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


    Texture2D GetImageFromWebcam()
    {
        var image = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBAFloat, false);
        image.SetPixels(webcamTexture.GetPixels());
        image.Apply();

        return image;
    }


    async Task ProcessImages()
    {
        float scaleX = webcamTexture.width / 640f;
        float scaleY = webcamTexture.height / 640f;

        while (true)
        {
            try
            {
                if (!webcamTexture)
                {
                    Thread.Sleep(100);
                    continue;
                }

                if (modelInput)
                {
                    Destroy(modelInput);
                }

                modelInput = GetImageFromWebcam();

                var pose = PassthroughCameraUtils.GetCameraPoseInWorld(PassthroughCameraEye.Left);
                var base64Image = Convert.ToBase64String(modelInput.EncodeToJPG());
                PythonBrick[] pythonBricks = await GetBricksFromServer(base64Image);

                lock(_bricksInternal)
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
                            _bricksInternal.Add(new Brick(brick.color,hit.point));

                            Debug.Log(brick);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }

    private async Task<PythonBrick[]> GetBricksFromServer(string base64Image)
    {
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
            _processingTask.Dispose();
        }


    }

    public override List<Brick> GetBricks()
    {
        var bricks = new List<Brick>();

        lock (_bricksInternal)
        {
            foreach (var brick in _bricksInternal)
            {
                bricks.Add(brick);
            }
        }

        return bricks;
    }
}

