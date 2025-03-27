using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnvironmentCreator : MonoBehaviour
{
    public ForestGenerator forestGenerator;
    public LakeController lakeController;

    public float radius;

    public Transform plane;

    public List<(Vector3,GameObject)> trees = new List<(Vector3,GameObject)>();

    void Start()
    {
        lakeController.radius = radius;

        trees.ForEach((x) => DestroyImmediate(x.Item2));
        trees = new List<(Vector3, GameObject)>();
        Debug.Log((int)(100 * plane.lossyScale.x));
        var genTrees = forestGenerator.GenerateForest(5,
            (int)(100 * plane.lossyScale.x),
            (int)(100 * plane.lossyScale.z),
            new Vector2(plane.lossyScale.x, plane.lossyScale.z) * 1 / 10f);

        foreach (var tree in genTrees)
        {
            trees.Add((tree.transform.position,tree));
        }
        
        foreach (var (pos,tree) in trees)
        {
            var newPoint = PlaneToWorldCoordinates(pos);
            
            tree.transform.position = newPoint;

            tree.transform.up = PlaneToWorldRotation(Vector3.up);
        }
       
    }



    Vector3 PlaneToWorldCoordinates(Vector3 position)
    {
        Matrix4x4 planeToWorld = Matrix4x4.TRS(plane.position,plane.rotation,Vector3.one);
        return planeToWorld.MultiplyPoint(position);
    }
    
    Vector3 PlaneToWorldRotation(Vector3 rotation)
    {
        Matrix4x4 planeToWorld = Matrix4x4.TRS(plane.position,plane.rotation,Vector3.one);
        return planeToWorld.MultiplyVector(rotation);
    }
}
