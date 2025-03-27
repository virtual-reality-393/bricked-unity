using UnityEngine;

public class LakeController : MonoBehaviour
{
    private static readonly int Spheres = Shader.PropertyToID("_Spheres");
    public Material waterMaterial;  // Reference to the material with the water shader
    public Vector3[] spheres;     // Array of spheres in the scene, representing the centers

    public float radius = 1;

    void Update()
    {
        // Create a list of sphere data (position + radius)
        Vector4[] sphereData = new Vector4[16];
        
        for (int i = 0; i < spheres.Length; i++)
        {
            // The radius is arbitrary, you can adjust it as needed
            sphereData[i] = new Vector4(spheres[i].x, spheres[i].y, spheres[i].z, radius);
        }

        // Set the sphere data to the shader material
        waterMaterial.SetVectorArray(Spheres, sphereData);
    }
}
