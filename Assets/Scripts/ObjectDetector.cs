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

    public abstract List<Brick> GetBricks();
}
