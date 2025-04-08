using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Sentis;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PredictionPerformance : MonoBehaviour
{
    public ModelAsset modelAsset;

    public Model model;

    public Texture2D modelInput;

    Worker objectDetectionWorker;

    int LAYERS_PER_FRAME = 1000;
    private readonly float CONFIDENCE_LEVEL = 0.4f;
    private readonly float NMS_THRESHOLD = 0.4f;

    void Start()
    {
        model = ModelLoader.Load(modelAsset);

        objectDetectionWorker = new Worker(model, BackendType.GPUCompute);
        StartCoroutine(Inference());

        
    }

    // Update is called once per frame
    IEnumerator Inference()
    {
        Stopwatch watch = new Stopwatch();
        watch.Reset();
        while (true)
        {
            watch.Start();
            
            var tf = new TextureTransform().SetDimensions(640, 640, 3);
            var modelInputTensor = new Tensor<float>(new TensorShape(1, 3, 640, 640));


            TextureConverter.ToTensor(modelInput, modelInputTensor, tf);
            var detectionScheduler = objectDetectionWorker.ScheduleIterable(modelInputTensor);

            int framesTaken = 0;
            while (detectionScheduler.MoveNext())
            {
                if (framesTaken % LAYERS_PER_FRAME == 0 && framesTaken > 0)
                {
                    yield return null;
                }

                framesTaken++;
            }

            var modelOut = (objectDetectionWorker.PeekOutput() as Tensor<float>).ReadbackAndClone();

            List<DetectionBox> bboxes = new List<DetectionBox>();
            for (int i = 0; i < modelOut.shape[2]; i++)
            {
                if (modelOut[0, 4, i] > CONFIDENCE_LEVEL)
                {
                    float x = modelOut[0, 0, i];
                    float y = modelOut[0, 1, i];
                    float w = modelOut[0, 2, i];
                    float h = modelOut[0, 3, i];


                    int x1 = (int)(x - w / 2);
                    int x2 = (int)(x + w / 2);

                    int y1 = (int)(640 - y - h / 2);
                    int y2 = (int)(640 - y + h / 2);

                    bboxes.Add(new DetectionBox(0,x1, y1, x2, y2));
                }
            }

            float scaleX = modelInput.width / 640f;
            float scaleY = modelInput.height / 640f;

            bboxes = ApplyNMS(bboxes);

            modelInputTensor.Dispose();

            watch.Stop();

            Debug.Log(watch.ElapsedMilliseconds);

            watch.Reset();
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

}
