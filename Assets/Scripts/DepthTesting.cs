using Meta.XR;
using Meta.XR.EnvironmentDepth;
using UnityEngine;

public class DepthTesting : MonoBehaviour
{
    public RenderTexture rend;

    // Update is called once per frame
    void Update()
    {
        var text = Shader.GetGlobalTexture(Shader.PropertyToID("_EnvironmentDepthTexture"));

        if(text)
        {
            Graphics.Blit(text, rend);
        }

    }

}
