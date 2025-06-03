using System;
using Meta.XR.EnvironmentDepth;
using Meta.XR;
using PassthroughCameraSamples;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public abstract class ObjectDetector : MonoBehaviour
{

    public static readonly ReadOnlyDictionary<int, string> DetectedLabelIdxToLabelName = new(new Dictionary<int, string>
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
    });
    
    [SerializeField]
    protected EnvironmentRaycastManager environmentRaycastManager;
    [SerializeField]
    protected WebCamTextureManager webCamTextureManager;
    protected WebCamTexture webcamTexture;

    protected readonly float CONFIDENCE_LEVEL = 0.4f;

    public EventHandler<ObjectDetectedEventArgs> OnObjectsDetected;
    public EventHandler<StackDetectedEventArgs> OnStacksDetected;

    public abstract List<DetectedObject> GetBricks();
    

    protected virtual void HandleBricksDetected(List<DetectedObject> detectedObjects)
    {
        OnObjectsDetected?.Invoke(this,new ObjectDetectedEventArgs(detectedObjects));
    }

    protected virtual void HandleStacksDetected(List<DetectedStack> detectedStacks)
    {
        OnStacksDetected?.Invoke(this,new StackDetectedEventArgs(detectedStacks));
    }
}


public class ObjectDetectedEventArgs : EventArgs
{
    public readonly List<DetectedObject> DetectedObjects;

    public ObjectDetectedEventArgs(List<DetectedObject> detectedObjects)
    {
        this.DetectedObjects = detectedObjects;
    }
}


public class StackDetectedEventArgs : EventArgs
{
    public readonly List<DetectedStack> DetectedStacks;

    public StackDetectedEventArgs(List<DetectedStack> detectedStacks)
    {
        this.DetectedStacks = detectedStacks;
    }
}

