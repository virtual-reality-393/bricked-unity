using UnityEngine;
using Unity.Sentis;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Meta.XR;
using Meta.XR.EnvironmentDepth;
using PassthroughCameraSamples;
using System.Security.Cryptography;
using TMPro;

public class LocalObjectDetector : ObjectDetector
{
    public ModelAsset objectDetector;

    Worker objectDetectionWorker;
    private readonly float NMS_THRESHOLD = 0.4f; // IoU threshold for NMS
    private readonly int LAYERS_PER_FRAME = 5;
    bool playing;

    private List<DetectedObject> bricksInternal;
    private Tensor<float> modelInputTensor;
    private TextureTransform tf;
    public Transform centerCam;
    public Transform canvas;


    void Start()
    {
        var detectionModel = ModelLoader.Load(objectDetector);
        bricksInternal = new List<DetectedObject>();
        objectDetectionWorker = new Worker(detectionModel, BackendType.GPUCompute);
        tf = new TextureTransform().SetDimensions(640, 640, 3);
        modelInputTensor = new Tensor<float>(new TensorShape(1, 3, 640, 640));
        SetWebCam();
    }

    // Update is called once per frame
    void Update()
    {
        if (webcamTexture == null)
        {
            SetWebCam();

        }
        
        if (webcamTexture != null && !playing)
        {
            StartCoroutine(ProcessImage());
            playing = true;
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
            webcamTexture = new WebCamTexture("QuickCam for Notebooks Deluxe"); // change device index to find correct one
            webcamTexture.Play();
        }
    }


    Texture2D GetImageFromWebcam()
    {
        var image = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBAFloat, false);
        image.SetPixels(webcamTexture.GetPixels());
        image.Apply();

        return image;
    }

    void DrawBoundingBox(Texture2D image, DetectionBox bbox)
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

    IEnumerator ProcessImage()
    {
        List<GameObject> debugObjects = new List<GameObject>();
        while (true)
        {
            
            Debug.LogError("1");
            var pose = PassthroughCameraUtils.GetCameraPoseInWorld(PassthroughCameraEye.Left);
            TextureConverter.ToTensor(webcamTexture, modelInputTensor, tf);
            var detectionScheduler = objectDetectionWorker.ScheduleIterable(modelInputTensor);
            Debug.LogError("2");
            int framesTaken = 0;
            while (detectionScheduler.MoveNext())
            {
                if (framesTaken % LAYERS_PER_FRAME == 0 && framesTaken > 0)
                {
                    yield return null;
                }

                framesTaken++;
            }
            Debug.LogError("3");
            bricksInternal = new List<DetectedObject>();

            var modelOut = (objectDetectionWorker.PeekOutput() as Tensor<float>).ReadbackAndClone();

            List<DetectionBox> bboxes = new List<DetectionBox>();


            float maxConf = 0;
            for (int i = 0; i < modelOut.shape[2]; i++)
            {
                for (int j = 0; j < modelOut.shape[1]-4; j++)
                {
                    maxConf = Mathf.Max(maxConf, modelOut[0, 4 + j, i]);
                    if (modelOut[0, 4+j, i] > CONFIDENCE_LEVEL)
                    {
                        
                        float x = modelOut[0, 0, i];
                        float y = modelOut[0, 1, i];
                        float w = modelOut[0, 2, i];
                        float h = modelOut[0, 3, i];


                        int x1 = (int)(x - w / 2);
                        int x2 = (int)(x + w / 2);

                        int y1 = (int)(640 - y - h / 2);
                        int y2 = (int)(640 - y + h / 2);

                        bboxes.Add(new DetectionBox(j,x1, y1, x2, y2));
                    }
                }
            }
            Debug.LogError(maxConf);
            float scaleX = webcamTexture.width / 640f;
            float scaleY = webcamTexture.height / 640f;
            Debug.LogError("5");
            bboxes = ApplyNMS(bboxes);
            foreach (var bbox in bboxes)
            {
                var screenPoint = new Vector2Int((int)(bbox.GetCenter().x * scaleX), (int)((bbox.GetCenter().y) * scaleY));
                var camRay = PassthroughCameraUtils.ScreenPointToRayInCamera(PassthroughCameraEye.Left, screenPoint);
                var rayDirectionInWorld = pose.rotation * camRay.direction;

                if (environmentRaycastManager.Raycast(new Ray(pose.position, rayDirectionInWorld), out EnvironmentRaycastHit hit, 1000f))
                {

                    bricksInternal.Add(new DetectedObject(bbox.label,detectedLabelIdxToLabelName[bbox.label],hit.point));
                }
            }
            Debug.LogError("6");
            foreach (var v in debugObjects)
            {
                Destroy(v);
            }
            debugObjects =  new List<GameObject>();
            
            foreach (Transform t in canvas.transform)
            {
                Destroy(t.gameObject);
            }

            foreach (var v in bricksInternal)
            {
                AddText(v.labelName,v.worldPos,Color.magenta);
                debugObjects.Add(v.Draw());
                Debug.LogError("7");
            }
            HandleBricksDetected(GetBricks());

            yield return null;
        }
    }
    
    public void AddText(string text, Vector3 position, Color color)
    {
        // Create a new GameObject for the text and set its attributes.
        GameObject newGameObject = new GameObject();
        RectTransform rect = newGameObject.AddComponent<RectTransform>();
        rect.position = position + new Vector3(0,0.03f, 0);
        rect.rotation = Quaternion.identity;
        rect.LookAt(centerCam);
        rect.Rotate(Vector3.up, 180);
        rect.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        newGameObject.transform.SetParent(canvas.transform);
        TextMeshPro newText = newGameObject.AddComponent<TextMeshPro>();

        // Set specific TextMeshPro settings, extend this as you see fit.
        newText.text = text;
        newText.fontSize = 1;
        newText.alignment = TextAlignmentOptions.Center;
        newText.color = color;
    }

    private List<DetectionBox> ApplyNMS(List<DetectionBox> bboxes)
    {
        // Sort bounding boxes by confidence score (higher first)
        bboxes = bboxes.OrderByDescending(b => b.GetArea()).ToList();

        List<DetectionBox> result = new List<DetectionBox>();

        while (bboxes.Count > 0)
        {
            DetectionBox currentBox = bboxes[0];
            bboxes.RemoveAt(0);

            result.Add(currentBox);

            // Remove boxes that overlap with current box (IoU > NMS_THRESHOLD)
            bboxes = bboxes.Where(b => CalculateIoU(currentBox, b) < NMS_THRESHOLD).ToList();
        }

        return result;
    }

    private float CalculateIoU(DetectionBox box1, DetectionBox box2)
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
    }

    public override List<DetectedObject> GetBricks()
    {
        var res = new List<DetectedObject>();
        foreach (var brick in bricksInternal)
        {
            res.Add(brick);
        }
        return res;
    }
    

}