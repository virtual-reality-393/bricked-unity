using UnityEngine;

public class DepthPointCloud : MonoBehaviour
{
    public ComputeShader shader;
    
    private int kernelHandle;

    
    

    void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");
        
    }

    public void RenderPointCloud(Texture input, float size)
    {
        float scaleX = input.width / size;
        float scaleY = input.height / size;

        float scale = Mathf.Max(scaleX, scaleY);

        int offSetX = (int)(size * scale - input.width) / 2;
        int offSetY = (int)(size * scale - input.height) / 2;


        shader.SetTexture(kernelHandle, "Input", input);

        shader.Dispatch(kernelHandle, (int)size / 8, (int)size / 8, 1);
    }
}
