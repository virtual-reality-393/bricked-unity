using UnityEngine;
using Unity.Sentis;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class ObjectDetection : MonoBehaviour
{
    public ModelAsset objectDetector;
    public RenderTexture output;
    //public EnvironmentDepthManager environmentDepthManager;
    //public EnvironmentRaycastManager environmentRaycastManager;

    string base64Img = "aaaaaaa";

    PythonServer server;

    public Texture2D modelInput;
    public Transform testTransofmr;

    Worker objectDetectionWorker;
    private readonly float CONFIDENCE_LEVEL = 0.3f;
    private readonly float NMS_THRESHOLD = 0.4f; // IoU threshold for NMS

    public List<GameObject> brickObjs;

    WebCamTexture webcamTexture;

    public GameObject plsworkobject;

    bool playing;

    public bool step;

    int testRun;

    public async void Start()
    {
        var detectionModel = ModelLoader.Load(objectDetector); ;
        objectDetectionWorker = new Worker(detectionModel, BackendType.GPUCompute);
        //environmentDepthManager.enabled = true;
        SetWebCam();
        server = new PythonServer();
        await server.StartServer();
    }

    // Update is called once per frame
    void Update()
    {
        //if (webcamTexture == null)
        //{
        //    SetWebCam();

        //    if (webcamTexture != null && !playing)
        //    {
        //        StartCoroutine(ProcessImage());
        //        playing = true;
        //    }
        //}


        //if(testRun > 5)
        //{
        //    StartCoroutine(ProcessImage());
        //}
        //testRun++;

        if(server.started && !playing)
        {
            playing = true;

            ProcessImageOnServer();
        }
    }

    void SetWebCam()
    {

        //if (webCamTextureManager != null)
        //{
        //    if (webCamTextureManager.WebCamTexture != null)
        //    {
        //        webcamTexture = webCamTextureManager.WebCamTexture;
        //        webcamTexture.Play();
        //    }
        //}
        //else
        //{
        webcamTexture = new WebCamTexture("QuickCam for Notebooks Deluxe"); // change device index to find correct one

        Debug.Log(webcamTexture.isPlaying);
        Debug.Log(webcamTexture.isReadable);

        webcamTexture.Play();

        Debug.Log(webcamTexture.isPlaying);
        Debug.Log(webcamTexture.isReadable);
        //}
    }


    Texture2D GetImageFromWebcam()
    {
        var image = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBAFloat, false);
        image.SetPixels(webcamTexture.GetPixels());
        image.Apply();

        return image;
    }

    void DrawBoundingBox(Texture2D image, BoundingBox bbox)
    {
        // Ensure that the bounding box is within the image's bounds
        int x1 = Mathf.Max(bbox.x1, 0);
        int y1 = Mathf.Max(bbox.y1, 0);
        int x2 = Mathf.Min(bbox.x2, image.width);
        int y2 = Mathf.Min(bbox.y2, image.height);

        // Calculate the width and height of the bounding box within valid bounds
        int width = x2 - x1;
        int height = y2 - y1;

        // If the bounding box is valid (non-zero size), draw it
        if (width > 0 && height > 0)
        {
            int centerX = x1 + width / 2;
            int centerY = y1 + height / 2;
            var color = image.GetPixel(centerX, centerY);
            var colorTest = new Color32[width * height];
            for (int i = 0; i < colorTest.Length; i++)
            {
                colorTest[i] = color; // Set to red color for the bounding box
            }

            // Draw the bounding box on the image
            image.SetPixels32(x1, y1, width, height, colorTest);
        }
    }


    async Task ProcessImageOnServer()
    {
        while(true)
        {

            //var pose = PassthroughCameraUtils.GetCameraPoseInWorld(PassthroughCameraEye.Left);
            //var tf = new TextureTransform().SetDimensions(640, 640, 3);
            //var modelInputTensor = new Tensor<float>(new TensorShape(1, 3, 640, 640));

            if (modelInput)
            {
                Destroy(modelInput);
            }

            modelInput = GetImageFromWebcam();

            var base64Image = Convert.ToBase64String(modelInput.EncodeToJPG());


            Debug.Log("Sending Message");
            Debug.Log(base64Image.Length);
            await server.SendMessage(base64Image);

            await server.ReceiveMessages();
            Debug.Log("Received Message");
        }
    }

    //IEnumerator ProcessImage()
    //{
    //    brickObjs = new();
    //    int layersPerFrame = 5;
    //    while (true)
    //    {
    //        var prevActive = RenderTexture.active;
    //        if (modelInput)
    //        {
    //            Destroy(modelInput);
    //        }

    //        var pose = PassthroughCameraUtils.GetCameraPoseInWorld(PassthroughCameraEye.Left);
    //        var tf = new TextureTransform().SetDimensions(640, 640, 3);
    //        var modelInputTensor = new Tensor<float>(new TensorShape(1, 3, 640, 640));

    //        modelInput = GetImageFromWebcam();

    //        TextureConverter.ToTensor(modelInput, modelInputTensor, tf);
    //        var detectionScheduler = objectDetectionWorker.ScheduleIterable(modelInputTensor);

    //        int framesTaken = 0;
    //        while (detectionScheduler.MoveNext())
    //        {
    //            if (framesTaken % layersPerFrame == 0 && framesTaken > 0)
    //            {
    //                testRun = 0;
    //                yield return null;
    //            }

    //            framesTaken++;
    //        }

    //        var modelOut = (objectDetectionWorker.PeekOutput() as Tensor<float>).ReadbackAndClone();

    //        List<BoundingBox> bboxes = new List<BoundingBox>();
    //        for (int i = 0; i < modelOut.shape[2]; i++)
    //        {
    //            if (modelOut[0, 4, i] > CONFIDENCE_LEVEL)
    //            {
    //                float x = modelOut[0, 0, i];
    //                float y = modelOut[0, 1, i];
    //                float w = modelOut[0, 2, i];
    //                float h = modelOut[0, 3, i];


    //                int x1 = (int)(x - w / 2);
    //                int x2 = (int)(x + w / 2);

    //                int y1 = (int)(640 - y - h / 2);
    //                int y2 = (int)(640 - y + h / 2);

    //                bboxes.Add(new BoundingBox(x1, y1, x2, y2));
    //            }
    //        }

    //        float scaleX = webcamTexture.width / 640f;
    //        float scaleY = webcamTexture.height / 640f;

    //        foreach (var v in brickObjs)
    //        {
    //            Destroy(v);
    //        }

    //        brickObjs = new();

    //        bboxes = ApplyNMS(bboxes);


    //        foreach (var bbox in bboxes)
    //        {
    //            var screenPoint = new Vector2Int((int)(bbox.GetCenter().x * scaleX), (int)((bbox.GetCenter().y) * scaleY));
    //            var camRay = PassthroughCameraUtils.ScreenPointToRayInCamera(PassthroughCameraEye.Left, screenPoint);
    //            var rayDirectionInWorld = pose.rotation * camRay.direction;

    //            if(environmentRaycastManager.Raycast(new Ray(pose.position, rayDirectionInWorld),out EnvironmentRaycastHit hit,1000f))
    //            {
    //                var hitObj = Instantiate(plsworkobject, hit.point,Quaternion.identity);

    //                hitObj.transform.forward = camRay.direction;

    //                brickObjs.Add(hitObj);
    //            }
    //        }

    //        modelInputTensor.Dispose();
    //        modelOut.Dispose();

    //        RenderTexture.active = prevActive;

    //        yield return null;
    //    }
    //}

    private List<BoundingBox> ApplyNMS(List<BoundingBox> bboxes)
    {
        // Sort bounding boxes by confidence score (higher first)
        bboxes = bboxes.OrderByDescending(b => b.GetArea()).ToList();

        List<BoundingBox> result = new List<BoundingBox>();

        while (bboxes.Count > 0)
        {
            BoundingBox currentBox = bboxes[0];
            bboxes.RemoveAt(0);

            result.Add(currentBox);

            // Remove boxes that overlap with current box (IoU > NMS_THRESHOLD)
            bboxes = bboxes.Where(b => CalculateIoU(currentBox, b) < NMS_THRESHOLD).ToList();
        }

        return result;
    }

    private float CalculateIoU(BoundingBox box1, BoundingBox box2)
    {
        Rect rect1 = box1.ToRect();
        Rect rect2 = box2.ToRect();

        float intersectionArea = Mathf.Max(0, Mathf.Min(rect1.xMax, rect2.xMax) - Mathf.Max(rect1.xMin, rect2.xMin)) *
                                Mathf.Max(0, Mathf.Min(rect1.yMax, rect2.yMax) - Mathf.Max(rect1.yMin, rect2.yMin));

        float box1Area = rect1.width * rect1.height;
        float box2Area = rect2.width * rect2.height;

        float unionArea = box1Area + box2Area - intersectionArea;

        return intersectionArea / unionArea; // IoU = intersection area / union area
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
    }
}


public struct BoundingBox
{
    public readonly int x1;
    public readonly int y1;
    public readonly int x2;
    public readonly int y2;

    public readonly int Width => x2 - x1;
    public readonly int Height => y2 - y1;

    public BoundingBox(int x1, int y1, int x2, int y2)
    {

        if (x1 >= x2 || y1 > y2)
        {
            throw new ArgumentException("x1 must be smaller than x2 and y1 must be smaller than y2");
        }

        this.x1 = x1;
        this.y1 = y1;
        this.x2 = x2;
        this.y2 = y2;
    }

    public Vector2Int GetCenter()
    {
        return new Vector2Int(x1+(x2-x1)/2, y1+(y2-y1)/2);
    }

    public int GetArea()
    {
        return (x2 - x1) * (y2 - y1);
    }

    public BoundingBox Intersect(BoundingBox otherBox)
    {
        int newx1 = Math.Max(this.x1, otherBox.x1);
        int newx2 = Math.Min(this.x2, otherBox.x2);

        int newy1 = Math.Max(this.y1, otherBox.y1);
        int newy2 = Math.Min(this.y2, otherBox.y2);

        if (newx1 > newx2 || newy1 > newy2)
        {
            return new BoundingBox();
        }

        return new BoundingBox(newx1, newy1, newx2, newy2);
    }


    public Rect ToRect()
    {
        return new Rect(x1, y1, x2 - x1, y2 - y1);
    }
}
