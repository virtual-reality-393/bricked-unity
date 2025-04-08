using System;
using Meta.XR.EnvironmentDepth;
using Meta.XR;
using PassthroughCameraSamples;
using System.Collections.Generic;
using UnityEngine;

public abstract class ObjectDetector : MonoBehaviour
{

    public static Dictionary<int, string> detectedLabelIdxToLabelName = new Dictionary<int, string>
    {
        {0,"red"},
        {1,"green"},
        {2,"blue"},
        {3,"yellow"},
        {4,"big penguin"},
        {5,"small penguin"},
        {6,"lion"},
        {7,"sheep"},
        {8,"pig"},
        {9,"human"},
    };
    [SerializeField]
    protected EnvironmentDepthManager environmentDepthManager;
    [SerializeField]
    protected EnvironmentRaycastManager environmentRaycastManager;
    [SerializeField]
    protected WebCamTextureManager webCamTextureManager;
    protected WebCamTexture webcamTexture;

    protected readonly float CONFIDENCE_LEVEL = 0.3f;
    
    public delegate void ObjectDetectedEventHandler(ObjectDetectedEventArgs plane);

    public EventHandler<ObjectDetectedEventArgs> OnBricksDetected;

    public abstract List<DetectedObject> GetBricks();
    

    protected virtual void HandleBricksDetected(List<DetectedObject> bricks)
    {
        OnBricksDetected?.Invoke(this,new ObjectDetectedEventArgs(bricks));
    }
}


public class ObjectDetectedEventArgs : EventArgs
{
    public List<DetectedObject> Bricks;

    public ObjectDetectedEventArgs(List<DetectedObject> bricks)
    {
        this.Bricks = bricks;
    }
}

