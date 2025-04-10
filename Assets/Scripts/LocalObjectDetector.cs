using UnityEngine;
using Unity.Sentis;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using Meta.XR;
using PassthroughCameraSamples;
using TMPro;

public class LocalObjectDetector : ObjectDetector
{
    public ModelAsset objectDetector;

    private Worker _objectDetectionWorker;
    private bool _playing;
    private List<DetectedObject> _internalDetection;
    private Tensor<float> _modelInputTensor;
    private TextureTransform _tf;
    
    private const float NmsThreshold = 0.4f; // IoU threshold for NMS
    private const int LayersPerFrame = 5;


    void Start()
    {
        var detectionModel = ModelLoader.Load(objectDetector);
        _internalDetection = new List<DetectedObject>();
        _objectDetectionWorker = new Worker(detectionModel, BackendType.GPUCompute);
        _tf = new TextureTransform().SetDimensions(640, 640, 3);
        _modelInputTensor = new Tensor<float>(new TensorShape(1, 3, 640, 640));
        SetWebCam();
    }

    // Update is called once per frame
    void Update()
    {
        if (webcamTexture == null)
        {
            SetWebCam();

        }
        
        if (webcamTexture != null && !_playing)
        {
            StartCoroutine(ProcessImage());
            _playing = true;
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
    

    IEnumerator ProcessImage()
    {
        while (true)
        {
            var pose = PassthroughCameraUtils.GetCameraPoseInWorld(PassthroughCameraEye.Left);
            TextureConverter.ToTensor(webcamTexture, _modelInputTensor, _tf);
            var detectionScheduler = _objectDetectionWorker.ScheduleIterable(_modelInputTensor);

            int framesTaken = 0;
            while (detectionScheduler.MoveNext())
            {
                if (framesTaken % LayersPerFrame == 0 && framesTaken > 0)
                {
                    yield return null;
                }

                framesTaken++;
            }

            _internalDetection = new List<DetectedObject>();

            var modelOut = (_objectDetectionWorker.PeekOutput() as Tensor<float>).ReadbackAndClone();

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

            float scaleX = webcamTexture.width / 640f;
            float scaleY = webcamTexture.height / 640f;
            
            bboxes = ApplyNMS(bboxes);
            foreach (var bbox in bboxes)
            {
                var screenPoint = new Vector2Int((int)(bbox.GetCenter().x * scaleX), (int)((bbox.GetCenter().y) * scaleY));
                var camRay = PassthroughCameraUtils.ScreenPointToRayInCamera(PassthroughCameraEye.Left, screenPoint);
                var rayDirectionInWorld = pose.rotation * camRay.direction;

                if (environmentRaycastManager.Raycast(new Ray(pose.position, rayDirectionInWorld), out EnvironmentRaycastHit hit, 1000f))
                {

                    _internalDetection.Add(new DetectedObject(bbox.label,DetectedLabelIdxToLabelName[bbox.label],hit.point));
                }
            }

            HandleBricksDetected(GetBricks());

            yield return null;
        }
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
            bboxes = bboxes.Where(b => CalculateIoU(currentBox, b) < NmsThreshold).ToList();
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
        foreach (var brick in _internalDetection)
        {
            res.Add(brick);
        }
        return res;
    }
    

}