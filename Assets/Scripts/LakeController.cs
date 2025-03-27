using UnityEngine;

public class LakeController : MonoBehaviour
{
    private static readonly int Spheres = Shader.PropertyToID("_Spheres");
    public Material waterMaterial;  // Reference to the material with the water shader
    public Transform[] spheres;     // Array of spheres in the scene, representing the centers

    [HideInInspector]public float radius = 0;

    void Update()
    {
        // Create a list of sphere data (position + radius)
        Vector4[] sphereData = new Vector4[spheres.Length];
        
        for (int i = 0; i < spheres.Length; i++)
        {
            // The radius is arbitrary, you can adjust it as needed
            sphereData[i] = new Vector4(spheres[i].position.x, spheres[i].position.y, spheres[i].position.z, radius);
        }

        // Set the sphere data to the shader material
        waterMaterial.SetVectorArray(Spheres, sphereData);
    }
}
