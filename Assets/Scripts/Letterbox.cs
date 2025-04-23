using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Letterbox : MonoBehaviour
{
    public ComputeShader shader;

    private int kernelHandle;
    private Texture2D _img;
    private RenderTexture _res;

    void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");

        _res = new RenderTexture(640,640,24)
        {
            format = RenderTextureFormat.ARGBFloat,
            enableRandomWrite = true
        };
        _res.Create();

        shader.SetTexture(kernelHandle, "Result", _res);
        
    }
    
    public Texture2D LoadImage(string path)
    {
        Texture2D text =  new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
        text.LoadImage(File.ReadAllBytes(path));
        return text;
    }

    public RenderTexture ComputeProcess(Texture input, float size)
    {
        float scaleX = input.width / size;
        float scaleY = input.height / size;

        float scale = Mathf.Max(scaleX, scaleY);

        int offSetX = (int)(size*scale - input.width) / 2;
        int offSetY = (int)(size*scale - input.height) / 2;
        
        
        shader.SetFloat( "Scale", scale);
        shader.SetFloat( "Height", input.height);
        shader.SetFloat( "Width", input.width);
        shader.SetInt("OffsetX", offSetX);
        shader.SetInt("OffsetY", offSetY);
        
        
        shader.SetTexture(kernelHandle, "Input", input);

        shader.Dispatch(kernelHandle, (int)size / 8, (int)size / 8, 1);

        return _res;
    }

    public Vector2 RescalePoint(Vector2 point, Texture input, float size)
    {
        float scaleX = input.width / size;
        float scaleY = input.height / size;

        float scale = Mathf.Max(scaleX, scaleY);

        int offSetX = (int)(size - input.width/scale) / 2;
        int offSetY = (int)(size - input.height/scale) / 2;
        
        
        return new Vector2((point.x-offSetX)*scale,(point.y-offSetY)*scale);
    }
    
    public Vector2Int RescalePoint(Vector2Int point, Texture input, float size)
    {
        var res =RescalePoint(new Vector2(point.x,point.y),input,size);
        return new Vector2Int((int)res.x,(int)res.y);
    }


    

}
