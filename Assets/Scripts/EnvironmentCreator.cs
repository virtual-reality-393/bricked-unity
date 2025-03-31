using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class EnvironmentCreator : MonoBehaviour
{
    public PlaneSpwaner spawner;
    public ForestGenerator forestGenerator;
    public LakeController lakeController;
    public ObjectDetector objectDetector;
    
    
    
    public Material planeMaterial;

    public float scale;


    public List<(Vector3,GameObject)> trees = new List<(Vector3,GameObject)>();

    void Awake()
    {
        spawner.OnPlaneSpawned += OnPlaneSpawned; 
        objectDetector.OnBricksDetected += OnBricksDetected;
    }

    private void OnBricksDetected(object sender, BricksDetectedEventArgs e)
    {
        HandleLakeBricks(e);
        HandleForestBricks(e);
    }

    private void HandleForestBricks(BricksDetectedEventArgs e)
    {
        var bricks = e.Bricks;
        
        foreach (var treeObj in trees)
        {
            var tree = treeObj.Item2;
                    
            tree.SetActive(false);
            
            foreach (var brick in bricks)
            {
                if (brick.colorName == "green")
                {
                    if (Vector3.Distance(tree.transform.position, brick.worldPos) < 0.2f)
                    {
                        foreach (var sphere in lakeController.spheres)
                        {
                            if (Vector3.Distance(tree.transform.position, sphere) >= 0.1)
                            {
                                tree.SetActive(true);
                            }
                        }
                    }
                }
            }
            
        }
        
    }

    private void HandleLakeBricks(BricksDetectedEventArgs e)
    {
        var bricks = e.Bricks;
        Vector3[] blueBricks = new Vector3[16];
        int i = 0;
        foreach (var brick in bricks)
        {
            if (brick.colorName == "blue")
            {
                i++;
                blueBricks[i] = brick.worldPos;
            }
        }
        
        lakeController.spheres =  blueBricks;
    }

    private void OnPlaneSpawned(object sender, PlaneSpawnedEventArgs e)
    {
        Transform plane = e.Plane.transform;
        
        plane.GetComponent<Renderer>().material = planeMaterial;

        trees.ForEach((x) => DestroyImmediate(x.Item2));
        trees = new List<(Vector3, GameObject)>();
        Debug.Log((int)(100 * plane.localScale.x));
        var genTrees = forestGenerator.GenerateForest(5,
            (int)(scale*100 * plane.localScale.x),
            (int)(scale*100 * plane.localScale.z),
             1 / (10f*scale));
        
        foreach (var tree in genTrees)
        {
            trees.Add((tree.transform.position,tree));
        }
        
        foreach (var (pos,tree) in trees)
        {
            var newPoint = PlaneToWorldCoordinates(pos,plane);
            
            tree.transform.position = newPoint;
        
            tree.transform.up = PlaneToWorldRotation(Vector3.up,plane);
            
            tree.transform.Rotate(tree.transform.up,360*Random.value);
            
            tree.SetActive(false);
        }
    }


    Vector3 PlaneToWorldCoordinates(Vector3 position,Transform plane)
    {
        Matrix4x4 planeToWorld = Matrix4x4.TRS(plane.position,plane.rotation,Vector3.one);
        return planeToWorld.MultiplyPoint(position);
    }
    
    Vector3 PlaneToWorldRotation(Vector3 rotation,Transform plane)
    {
        Matrix4x4 planeToWorld = Matrix4x4.TRS(plane.position,plane.rotation,Vector3.one);
        return planeToWorld.MultiplyVector(rotation);
    }
}
