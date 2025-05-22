using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using PassthroughCameraSamples;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

public class DepthPointCloud : MonoBehaviour
{
    public ComputeShader shader;

    public Material depthMat;
    
    private int kernelHandle;

    public Camera cam;

    public Mesh instanceMesh;
    public Material instanceMaterial;
    private GraphicsBuffer _graphicsBuffer;
    GraphicsBuffer.IndirectDrawIndexedArgs[] _commandData;
    private RenderParams _rendParam;
    private ComputeBuffer _output;
    public RenderTexture depthRT;
    public RenderTexture colorRT;
    private Texture2D depthTexture;
    
    


    public int resolution = 320;
    private int _threadCount = 4;
    private int _imageSize = 320;
    private int _frame;

    [SerializeField]
    protected WebCamTextureManager webCamTextureManager;
    protected WebCamTexture webcamTexture;
    private ComputeBuffer _outputColor;

    void Start()
    {
       
        // Create a 4x4 matrix to hold your 3x3 homography
        Matrix4x4 homography = new Matrix4x4();

        // Row 1
        homography.m00 = 3.74850964f;    // h11
        homography.m01 = 0.631605155f;   // h12
        homography.m02 = -525.363118f;   // h13
        homography.m03 = 0.0f;

// Row 2
        homography.m10 = -0.02346943f;   // h21
        homography.m11 = 5.61065027f;    // h22
        homography.m12 = -522.21897f;    // h23
        homography.m13 = 0.0f;

// Row 3
        homography.m20 = 0.00161094549f; // h31
        homography.m21 = 0.00455150462f; // h32
        homography.m22 = 1.0f;           // h33
        homography.m23 = 0.0f;

// Row 4 (not used but needs to be filled)
        homography.m30 = 0.0f;
        homography.m31 = 0.0f;
        homography.m32 = 0.0f;
        homography.m33 = 1.0f;
        kernelHandle = shader.FindKernel("CSMain");
        
        _output = new ComputeBuffer(resolution*resolution, sizeof(float)*4, ComputeBufferType.Default);
        shader.SetBuffer(kernelHandle, "Result", _output);
        
        _outputColor = new ComputeBuffer(resolution*resolution, sizeof(float)*4, ComputeBufferType.Default);
        shader.SetBuffer(kernelHandle, "ColorResult", _outputColor);
        
        shader.SetMatrix("Homography", homography);
        
        shader.SetInt("_Size", resolution);
        shader.SetInt("_Stride", _imageSize/resolution);
        
        // for (int i = 0; i < 256; i++)
        // {
        //     for (int j = 0; j < 256; j++)
        //     {
        //         var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //
        //         obj.transform.position = outputData[j + i * 256];
        //     }
        // }
        
        _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments,1,GraphicsBuffer.IndirectDrawIndexedArgs.size);
        _commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        // instanceMaterial.SetBuffer("_Positions",output);
        
        


        shader.SetTexture(kernelHandle, "Input", depthRT); 
        shader.SetTexture(kernelHandle, "_ColorInput",colorRT); 
        
        _rendParam = new RenderParams
        {
            material = instanceMaterial,
            worldBounds = new Bounds(Vector3.zero, Vector3.one*10000f),
            matProps = new MaterialPropertyBlock(),
            renderingLayerMask = RenderingLayerMask.defaultRenderingLayerMask,
        };
        _rendParam.matProps.SetBuffer("_Positions", _output);
        
        _commandData[0].indexCountPerInstance = instanceMesh.GetIndexCount(0);
        _commandData[0].instanceCount = (uint)resolution*(uint)resolution;
        _graphicsBuffer.SetData(_commandData);
        var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(PassthroughCameraEye.Left);
        shader.SetVector("_PrincipalPoint", intrinsics.PrincipalPoint);
        shader.SetVector("_FocalLength", intrinsics.FocalLength);
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

    private void Update()
    {
        if (webcamTexture == null)
        {
            SetWebCam();
            return;
        }
        
        Graphics.Blit(depthRT,depthRT,depthMat);
        Graphics.Blit(webcamTexture,colorRT);
        
        _rendParam.matProps.SetMatrix("_CamToWorldMatrix",cam.transform.localToWorldMatrix);
        
        shader.Dispatch(kernelHandle, resolution / _threadCount, resolution / _threadCount, 1);
        //
        //
        //
        // Graphics.RenderMeshIndirect(_rendParam,instanceMesh,_graphicsBuffer,1);

        if (++_frame == 100)
        {
            var depthData = new Vector4[resolution*resolution];
            _output.GetData(depthData);
            
            var colorData = new Vector4[resolution*resolution];
            _outputColor.GetData(colorData);

            StringBuilder sb = new StringBuilder();

            for (var index = 0; index < depthData.Length; index++)
            {
                var element = depthData[index];
                var colorElement = colorData[index];
                sb.Append(element.ToString("F5"));
                sb.Append("|");
                sb.Append(colorElement.ToString("F5"));
                if (index != depthData.Length - 1)
                {
                    sb.Append(";");
                }
            }

            File.WriteAllText(Path.Combine(Application.persistentDataPath, "cloud.txt"),sb.ToString());
            Debug.LogError($"Wrote to {Path.Combine(Application.persistentDataPath, "cloud.txt")}");
            
            
            Texture2D outputText = new Texture2D(320,320,TextureFormat.ARGB32, false);

            RenderTexture.active = depthRT;
            outputText.ReadPixels(new Rect(0, 0, 320, 320), 0, 0);
            outputText.Apply();

            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "depth.png"),outputText.EncodeToPNG());
            
            RenderTexture.active = colorRT;
            outputText.ReadPixels(new Rect(0, 0, 320, 320), 0, 0);
            outputText.Apply();

            
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "color.png"),outputText.EncodeToPNG());
        }
    }
    
}
