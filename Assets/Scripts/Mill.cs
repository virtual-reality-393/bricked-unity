using UnityEngine;

public class Mill : MonoBehaviour
{
    public float rotateSpeed;
    public Transform rotateObject;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        rotateObject.Rotate(Vector3.forward,rotateSpeed*Time.deltaTime);
    }
}
