using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class SandboxGame : BaseGame
{
    public ForestGenerator forestGenerator;
    public LakeController lakeController;
    public Material planeMaterial;
    public float scale;

    private List<(Vector3,GameObject)> _trees = new List<(Vector3,GameObject)>();
    
    public BrickManager brickManager;

    protected override void OnBricksDetected(object sender, ObjectDetectedEventArgs e)
    {
        HandleLakeBricks(e);
        HandleForestBricks(e);
    }

    protected override void Loop()
    {
        
    }


    private void HandleForestBricks(ObjectDetectedEventArgs e)
    {
        var bricks = e.DetectedObjects;
        
        foreach (var treeObj in _trees)
        {
            var tree = treeObj.Item2;
                    
            tree.SetActive(false);
            
            foreach (var brick in bricks)
            {
                if (brick.labelName == "green")
                {
                    if (Vector3.Distance(tree.transform.position, brick.worldPos) < 0.2f)
                    {
                        foreach (var sphere in lakeController.spheres)
                        {
                            if (Vector3.Distance(tree.transform.position, sphere) >= 0.3)
                            {
                                tree.SetActive(true);
                            }
                        }
                    }
                }
            }
            
        }
        
    }

    private void HandleLakeBricks(ObjectDetectedEventArgs e)
    {
        var bricks = e.DetectedObjects;
        Vector3[] blueBricks = new Vector3[16];
        int i = 0;
        foreach (var brick in bricks)
        {
            if (brick.labelName == "blue")
            {
                i++;
                blueBricks[i] = brick.worldPos;
            }
        }
        
        lakeController.spheres =  blueBricks;
    }

    protected override void OnPlaneSpawned(object sender, PlaneSpawnedEventArgs e)
    {
        base.OnPlaneSpawned(sender,e);
        var plane = e.Plane.transform;
        plane.GetComponent<Renderer>().material = planeMaterial;

        _trees.ForEach((x) => DestroyImmediate(x.Item2));
        _trees = new List<(Vector3, GameObject)>();
        Debug.LogError((int)(100 * plane.localScale.x));
        var genTrees = forestGenerator.GenerateForest(5,
            (int)(scale*100 * plane.localScale.x),
            (int)(scale*100 * plane.localScale.z),
             1 / (10f*scale));
        
        foreach (var tree in genTrees)
        {
            _trees.Add((tree.transform.position,tree));
        }
        
        foreach (var (pos,tree) in _trees)
        {
            var newPoint = PlaneToWorldCoordinates(pos,plane);
            
            tree.transform.position = newPoint;
        
            tree.transform.up = PlaneToWorldRotation(Vector3.up,plane);
            
            tree.transform.Rotate(tree.transform.up,360*Random.value);
            
            tree.transform.parent = plane;
            
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
