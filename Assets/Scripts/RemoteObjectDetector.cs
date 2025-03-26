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

    PythonServer server;
    List<Brick> bricksInternal;
    Task processingTask;
    bool playing;

    async void Start()
    {
        SetWebCam();
        server = new PythonServer();
        await server.StartServer();
    }

    // Update is called once per frame
    void Update()
    {
        if (webcamTexture == null)
        {
            SetWebCam();
        }

        if (server.started && !playing && webcamTexture != null)
        {
            playing = true;

            processingTask = ProcessImages();

            processingTask.Start();
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
                if (webcamTexture == null)
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

                lock(bricksInternal)
                {
                    bricksInternal.Clear();

                    foreach (var brick in pythonBricks)
                    {
                        var bbox = brick.GetBoundingBox();

                        var screenPoint = new Vector2Int((int)(bbox.GetCenter().x * scaleX), (int)((640 - bbox.GetCenter().y) * scaleY));
                        var camRay = PassthroughCameraUtils.ScreenPointToRayInCamera(PassthroughCameraEye.Left, screenPoint);
                        var rayDirectionInWorld = pose.rotation * camRay.direction;

                        if (environmentRaycastManager.Raycast(new Ray(pose.position, rayDirectionInWorld), out EnvironmentRaycastHit hit, 1000f))
                        {
                            bricksInternal.Add(new Brick(brick.color,hit.point));
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
        await server.SendMessage(base64Image);
        var result = (await server.ReceiveMessages())[0];
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

        server.OnApplicationQuit();


        if(processingTask != null)
        {
            processingTask.Dispose();
        }


    }

    public override List<Brick> GetBricks()
    {
        var bricks = new List<Brick>();

        lock (bricksInternal)
        {
            foreach (var brick in bricksInternal)
            {
                bricks.Add(brick);
            }
        }

        return bricks;
    }

    public override List<DebugBrick> GetDebugBricks()
    {
        throw new NotImplementedException();
    }
}

