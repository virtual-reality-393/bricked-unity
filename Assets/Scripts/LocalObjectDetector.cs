using System;
using UnityEngine;
using Unity.Sentis;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Text;
using Meta.XR;
using PassthroughCameraSamples;
using TMPro;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

public class LocalObjectDetector : ObjectDetector
{
    public ModelAsset stackDetector;
    public ModelAsset brickDetector;
    
    private Worker _stackDetectionWorker;
    private Worker _brickDetectionWorker;
    
    
    public Letterbox letterbox;
    private bool _playing;
    private List<DetectedObject> _internalDetection;
    private Tensor<float> _modelInputTensor;
    private TextureTransform _tf;
    
    private RenderTexture _modelInput;

    private const float NmsThreshold = 0.4f; // IoU threshold for NMS
    private const long TimePerFrame = 10;

    [SerializeField] private GameObject _debugRayPrefab;
    private PassthroughCameraIntrinsics _intrinsics;
    private List<GameObject> rays = new List<GameObject>();

    public Transform eye;

    private Vector3 _eyeOffset;
    public float timeTaken;
    
    public bool includeStacks;


    private int frame;

    void Start()
    {
        if (!EnvironmentRaycastManager.IsSupported)
        {
            Debug.LogError("EnvironmentRaycastManager is not supported: please read the official documentation to get more details. (https://developers.meta.com/horizon/documentation/unity/unity-depthapi-overview/)");
        }
        _intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(PassthroughCameraEye.Left);
        var brickDetectionModel = ModelLoader.Load(brickDetector);
        _internalDetection = new List<DetectedObject>();
        _brickDetectionWorker = new Worker(brickDetectionModel, BackendType.CPU);


        if (includeStacks)
        {
            var stackDetectionModel = ModelLoader.Load(stackDetector);
            _stackDetectionWorker = new Worker(stackDetectionModel, BackendType.CPU);
        }
        
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
            _modelInput = letterbox.ComputeProcess(webcamTexture,640f);
            StartCoroutine(ProcessImage());
            _playing = true;
        }

        if (webcamTexture != null)
        {
            _modelInput = letterbox.ComputeProcess(webcamTexture,640f);
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
        var pose = PassthroughCameraUtils.GetCameraPoseInWorld(PassthroughCameraEye.Left);
        eye.rotation = pose.rotation;
        Stopwatch sw = new Stopwatch();
        List<DetectionBox> bboxes = new List<DetectionBox>(256);
        while (true)
        {
            TextureConverter.ToTensor(_modelInput, _modelInputTensor, _tf);

            var position = eye.position;
            var rotation = eye.rotation;
            var brickDetectionScheduler = _brickDetectionWorker.ScheduleIterable(_modelInputTensor);
            sw.Restart();
            timeTaken = 0;
            while (brickDetectionScheduler.MoveNext())
            {
                if (!(sw.ElapsedMilliseconds - timeTaken >= TimePerFrame)) continue;
                yield return null;
                timeTaken = sw.ElapsedMilliseconds;
            }

            IEnumerator stackDetectionScheduler = null;

            if (includeStacks)
            {
                stackDetectionScheduler = _stackDetectionWorker.ScheduleIterable(_modelInputTensor);
            }
            
            sw.Restart();
            timeTaken = 0;
            while (includeStacks && stackDetectionScheduler.MoveNext())
            {
                if (!(sw.ElapsedMilliseconds - timeTaken >= TimePerFrame)) continue;
                yield return null;
                timeTaken = sw.ElapsedMilliseconds;
            }

            var brickOutput = _brickDetectionWorker.PeekOutput() as Tensor<float>;
            Tensor<float> stackOutput = null;

            brickOutput.ReadbackRequest();
            
            if (includeStacks)
            {
                stackOutput = _stackDetectionWorker.PeekOutput() as Tensor<float>;
                stackOutput.ReadbackRequest();
                while (!stackOutput.IsReadbackRequestDone())
                {
                    yield return null;
                }
            }

            while (!brickOutput.IsReadbackRequestDone())
            {
                yield return null;
            }
            
            var brickTensor = brickOutput.ReadbackAndClone();
            
            _internalDetection = GetBrickDetections(brickTensor,position,rotation);

            
            HandleBricksDetected(GetBricks());
            
            if (includeStacks)
            {
                var stackTensor = stackOutput.ReadbackAndClone();
                var stacks = GetStackDetections(stackTensor);
                
                HandleStacksDetected(stacks);
            }
            
            


            bboxes.Clear();
            _internalDetection.Clear();
            brickTensor.Dispose();
            frame++;
            yield return null;
        }
    }

    private List<DetectedStack> GetStackDetections(Tensor<float> stackTensor)
    {
        List<DetectionBox> bboxes = new List<DetectionBox>(); 
        
        var infoLen = stackTensor.shape[1];

        var pointCount = stackTensor.shape[2];

        var dataArr = stackTensor.DownloadToArray();

        for (int i = 0; i < pointCount; i++)
        {
            int idx = -1;

            float conf = CONFIDENCE_LEVEL;
            for (int j = i+4*pointCount; j < i+infoLen*pointCount; j+=pointCount)
            {
                if (dataArr[j] > conf)
                {
                    conf = dataArr[j];
                    idx = (j - i - 4*pointCount)/pointCount;
                }
            }

            if (idx != -1)
            {
                float x = dataArr[i];
                float y = dataArr[i+1*pointCount];
                float w = dataArr[i+2*pointCount];
                float h = dataArr[i+3*pointCount];
                
                int x1 = (int)(x - w / 2);
                int x2 = (int)(x + w / 2);
            
                int y1 = (int)(640 - y - h / 2);
                int y2 = (int)(640 - y + h / 2);
                    
                var p1 = letterbox.RescalePoint(new Vector2Int(x1, y1),webcamTexture,640f);
                var p2 = letterbox.RescalePoint(new Vector2Int(x2, y2),webcamTexture,640f);

                bboxes.Add(new DetectionBox(idx,conf,p1.x, p1.y, p2.x, p2.y));
            }
        }

        bboxes = ApplyNMS(bboxes);

        List<DetectedStack> res = new List<DetectedStack>();

        foreach (var bbox in bboxes)
        {
            res.Add(new DetectedStack(bbox.x1, bbox.x2, bbox.y1, bbox.y2));
            
        }
        return res;
    }

    private List<DetectedObject> GetBrickDetections(Tensor<float> brickTensor,Vector3 position, Quaternion rotation)
    {
        List<DetectionBox> bboxes = new List<DetectionBox>(); 
        
        var infoLen = brickTensor.shape[1];

        var pointCount = brickTensor.shape[2];

        var dataArr = brickTensor.DownloadToArray();

        for (int i = 0; i < pointCount; i++)
        {
            int idx = -1;

            float conf = CONFIDENCE_LEVEL;
            for (int j = i+4*pointCount; j < i+infoLen*pointCount; j+=pointCount)
            {
                if (dataArr[j] > conf)
                {
                    conf = dataArr[j];
                    idx = (j - i - 4*pointCount)/pointCount;
                }
            }

            if (idx != -1)
            {
                float x = dataArr[i];
                float y = dataArr[i+1*pointCount];
                float w = dataArr[i+2*pointCount];
                float h = dataArr[i+3*pointCount];
                
                int x1 = (int)(x - w / 2);
                int x2 = (int)(x + w / 2);
            
                int y1 = (int)(640 - y - h / 2);
                int y2 = (int)(640 - y + h / 2);
                    
                var p1 = letterbox.RescalePoint(new Vector2Int(x1, y1),webcamTexture,640f);
                var p2 = letterbox.RescalePoint(new Vector2Int(x2, y2),webcamTexture,640f);

                bboxes.Add(new DetectionBox(idx,conf,p1.x, p1.y, p2.x, p2.y));
            }
        }

        bboxes = ApplyNMS(bboxes);
            
        rays.ForEach(Destroy);
        rays.Clear();
            
        List<DetectedObject> res = new List<DetectedObject>();
        foreach (var bbox in bboxes)
        {
            var screenPoint = new Vector2Int(bbox.GetCenter().x, bbox.GetCenter().y);
            var directionInCamera = new Vector3
            {
                x = (screenPoint.x - _intrinsics.PrincipalPoint.x) / _intrinsics.FocalLength.x,
                y = (screenPoint.y - _intrinsics.PrincipalPoint.y) / _intrinsics.FocalLength.y,
                z = 1
            };
            // Debug.LogError($"Pose: {pose.rotation.eulerAngles}");
            // Debug.LogError($"Eye: {eye.rotation.eulerAngles}");
            // Debug.LogError($"Diff {eye.rotation.eulerAngles-pose.rotation.eulerAngles}");
            var ray = new Ray(position,rotation*directionInCamera);




    
            if (environmentRaycastManager.Raycast(ray, out EnvironmentRaycastHit hit, 1000f))
            {
                // var newRay = Instantiate(_debugRayPrefab);
                // newRay.GetComponent<LineRenderer>().SetPositions(new Vector3[] {ray.origin, ray.origin + ray.direction*5f});
                //
                //
                // // var newRay = Instantiate(GameManager.Instance.brickPrefab, ray.origin+ray.direction, Quaternion.identity);
                // // newRay.transform.localScale *= 0.3f;
                // // newRay.transform.forward = ray.direction;
                //
                // rays.Add(newRay);
                //
                res.Add(new DetectedObject(bbox.label,DetectedLabelIdxToLabelName[bbox.label],hit.point,bbox.GetCenter()));
            }
        }

        return res;
    }
    
    private string GetRawImage()
    {
        var modelInput = _modelInputTensor.ReadbackAndClone();
        // StringBuilder sb = new StringBuilder();
        
        for (int i = 0; i < 640; i++)
        {
            Debug.LogError($"line {i}:");
            StringBuilder sb = new StringBuilder();
            for (int j = 0; j < 640; j++)
            {
                sb.Append(modelInput[0,0,i, j]);
                sb.Append(" ");
                sb.Append(modelInput[0,1,i, j]);
                sb.Append(" ");
                sb.Append(modelInput[0,2,i, j]);
                if (j != 639)
                {
                    sb.Append("|");
                }

                if (j % 8 == 0)
                {
                    Debug.LogError(sb.ToString());
                    sb  = new StringBuilder();
                }
            }
            Debug.LogError(sb.ToString());

            
        }

        return "sb.ToString();";
    }
    
    private List<DetectionBox> ApplyNMS(List<DetectionBox> bboxes)
    {
        // Sort bounding boxes by confidence score (higher first)
        bboxes = bboxes.OrderByDescending(b => b.conf).ToList();

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