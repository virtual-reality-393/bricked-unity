using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ForestGenerator : MonoBehaviour
{
    public GameObject treePrefab;

    public List<GameObject> GenerateForest(float density, int width, int height, float scale)
    {
        List<GameObject> trees = new();
        var points = DiskSampling.GenerateDiskSamples(density,10,width,height);
        foreach (var point in points)
        {
            var newPoint = new Vector3(point.x, 0, point.y) - new Vector3(width/2f,0,height/2f);
            var tree=Instantiate(treePrefab, treePrefab.transform.position+new Vector3(newPoint.x,0,newPoint.z)*scale, Quaternion.identity);

            tree.transform.Rotate(0,360*Random.value,0);
            
            trees.Add(tree);
        }

        return trees;
    }
}
