using PassthroughCameraSamples;
using System.Collections.Generic;
using UnityEngine;

public class CameraRaycast : MonoBehaviour
{
    public Camera cam;
    Vector3 pos = new Vector3(200, 200, 0);
    public List<GameObject> hitObjects;


    void Start()
    {
        hitObjects = new List<GameObject>();
        pos = new Vector3(Screen.width / 2, Screen.height / 2);
    }

    void Update()
    {
        
    }
}
