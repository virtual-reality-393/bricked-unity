using PassthroughCameraSamples;
using UnityEngine;

public class WebcamRender : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public WebCamTextureManager textureManager;

    public bool started;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!started)
        {
            GetComponent<Renderer>().material.mainTexture = textureManager.WebCamTexture;
            textureManager.WebCamTexture.Play();
            started = true;
        }
    }
}
