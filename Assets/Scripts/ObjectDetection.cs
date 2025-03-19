using UnityEngine;
using Unity.Sentis;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;
using PassthroughCameraSamples;

public class ObjectDetection : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public ModelAsset objectDetector;
    public WebCamTextureManager webCamTextureManager;
    public RenderTexture output;
 
    public Texture2D modelInput;

    Texture2D modelAnnotation;

    Worker objectDetectionWorker;
    private readonly float CONFIDENCE_LEVEL = 0.3f;

    WebCamTexture webcamTexture;

    bool playing;

    public bool step;

    public async void Start()
    {
        var detectionModel = ModelLoader.Load(objectDetector);

        objectDetectionWorker = new Worker(detectionModel, BackendType.GPUCompute);

        webcamTexture = new WebCamTexture(WebCamTexture.devices[1].name); // change device index to find correct one

    }

    // Update is called once per frame
    void Update()
    {
        if(webcamTexture != null)
        {
            var prevActive = RenderTexture.active;
            if (!playing)
            {
                //webcamTexture = webCamTextureManager.WebCamTexture;
                webcamTexture.Play();
                playing = true;
            }

            if (modelInput)
            {
                Destroy(modelInput);
            }
            modelInput = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBAFloat,false);
            modelInput.SetPixels(webcamTexture.GetPixels());
            modelInput.Apply();

            var tf = new TextureTransform().SetDimensions(640,640,3);

            var modelInputTensor = new Tensor<float>(new TensorShape(1, 3, 640, 640));
            TextureConverter.ToTensor(modelInput, modelInputTensor, tf);
            objectDetectionWorker.Schedule(modelInputTensor);

            var modelOut = (objectDetectionWorker.PeekOutput() as Tensor<float>).ReadbackAndClone();

            TextureConverter.RenderToTexture(modelInputTensor, output, tf);

            List<BoundingBox> bboxes = new List<BoundingBox>();
            for (int i = 0; i < modelOut.shape[2]; i++)
            {
                if (modelOut[0,4,i] > CONFIDENCE_LEVEL)
                {
                    float x = modelOut[0, 0, i];
                    float y = modelOut[0, 1, i];
                    float w = modelOut[0, 2, i];
                    float h = modelOut[0, 3, i];


                    int x1 = (int)(x - w / 2);
                    int x2 = (int)(x + w / 2);

                    int y1 = (int)(640-y - h / 2);
                    int y2 = (int)(640-y + h / 2);

                    bboxes.Add(new BoundingBox(x1, y1, x2, y2));
                }
            }

            

            if(modelAnnotation)
            {
                Destroy(modelAnnotation);
            }

            RenderTexture.active = output;
            modelAnnotation = new Texture2D(640, 640, TextureFormat.RGBA32, false);
            modelAnnotation.ReadPixels(new Rect(0,0,640,640),0,0);
            foreach (var bbox in bboxes)
            {
                var colorTest = new Color32[bbox.Width*bbox.Height];
                modelAnnotation.SetPixels32(bbox.x1,bbox.y1,bbox.Width,bbox.Height,colorTest);
            }

            modelAnnotation.Apply();
            GetComponent<Renderer>().material.mainTexture = modelAnnotation;

            modelInputTensor.Dispose();

            modelOut.Dispose();

            RenderTexture.active = prevActive;
        }

    }

    private void OnApplicationQuit()
    {
        webcamTexture.Stop();

        Destroy(webcamTexture);
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

        if(x1 >= x2 || y1 > y2)
        {
            throw new ArgumentException("x1 must be smaller than x2 and y1 must be smaller than y2");
        }

        this.x1 = x1;
        this.y1 = y1;
        this.x2 = x2;
        this.y2 = y2;
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
        return new Rect(x1,y1,x2-x1,y2-y1);
    }
}
