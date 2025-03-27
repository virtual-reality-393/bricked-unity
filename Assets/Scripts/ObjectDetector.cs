using System;
using Meta.XR.EnvironmentDepth;
using Meta.XR;
using PassthroughCameraSamples;
using System.Collections.Generic;
using UnityEngine;

public abstract class ObjectDetector : MonoBehaviour
{
    [SerializeField]
    protected EnvironmentDepthManager environmentDepthManager;
    [SerializeField]
    protected EnvironmentRaycastManager environmentRaycastManager;
    [SerializeField]
    protected WebCamTextureManager webCamTextureManager;
    protected Texture2D modelInput;
    protected WebCamTexture webcamTexture;

    protected readonly float CONFIDENCE_LEVEL = 0.3f;
    
    public delegate void BricksDetectedEventHandler(BricksDetectedEventArgs plane);

    public EventHandler<BricksDetectedEventArgs> OnBricksDetected;

    public abstract List<Brick> GetBricks();

    public abstract List<DebugBrick> GetDebugBricks();

    protected virtual void HandleBricksDetected(List<Brick> bricks)
    {
        OnBricksDetected?.Invoke(this,new BricksDetectedEventArgs(bricks));
    }
}


public class BricksDetectedEventArgs : EventArgs
{
    public List<Brick> Bricks;

    public BricksDetectedEventArgs(List<Brick> bricks)
    {
        this.Bricks = bricks;
    }
}

