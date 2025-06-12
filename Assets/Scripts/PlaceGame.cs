//using Meta.XR.MRUtilityKit;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;

//public class PlaceGame : MonoBehaviour
//{
//    public ObjectDetector objectDetection;
//    public GameObject spawnPositions;

//    private FindSpawnPositions _findSpawnPositions;


//    public float Distans = 0.05f;

//    bool taskComplet = true;

//    Dictionary<string, int> bricksInFrame = new Dictionary<string, int>();

//    string state = "setup";
//    string[] colors = { "red", "green", "blue", "yellow" };

//    List<GameObject> drawnBricks = new List<GameObject>();

//    float[] dists = new float[4] { 100, 100, 100, 100 };

//    List<DetectedObject> bricks = new List<DetectedObject>();


//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {
//        _findSpawnPositions = spawnPositions.GetComponent<FindSpawnPositions>();
//        objectDetection.OnObjectsDetected += HandleBricksDetected;
//    }

//    private void HandleBricksDetected(object sender, ObjectDetectedEventArgs e)
//    {
//        bricks = new List<DetectedObject>();
//        e.DetectedObjects.ForEach(brick =>
//        {
//            if (colors.Contains(brick.labelName))
//            {
//                bricks.Add(brick);
//            }
//        });
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        if (state == "setup")
//        {
//            Setup();
//        }
//        else if (state == "play")
//        {
//            Play();
//        }
//    }


//    private void Setup()
//    {
        
//        ResetBricksInFrame();
//        DestroySpawnPositions();
 
//        drawnBricks.ForEach(Destroy);
//        drawnBricks.Clear();

//        foreach (var brick in bricks)
//        {
//            bricksInFrame[brick.labelName]++;
//            GameObject cube = brick.Draw();
//            drawnBricks.Add(cube);

//        }

//        if ((bricksInFrame["red"] == 1 && bricksInFrame["green"] == 1 && bricksInFrame["blue"] == 1 && bricksInFrame["yellow"] == 1))// || bricks.Count == 4)
//        {
//            drawnBricks.ForEach(Destroy);
//            drawnBricks.Clear();
//            state = "play";
//        }
//    }

//    private void Play()
//    {
//        //If the task is completed, choose new colors
//        if (taskComplet)
//        {
//            _findSpawnPositions.StartSpawn();

//            for (int i = 0; i < spawnPositions.transform.childCount; i++)
//            {
//                GameObject point = spawnPositions.transform.GetChild(i).gameObject;
//                point.GetComponent<Renderer>().material.color = GameUtils.nameToColor[colors[i]];
//            }
//            dists = new float[4] { 100, 100, 100, 100 };
//            taskComplet = false;
//        }
//        else
//        {
//            for (int i = 0; i < spawnPositions.transform.childCount; i++)
//            {
//                DetectedObject detectedObject = GameUtils.GetBrickWithColor(bricks, colors[i]);
//                if (detectedObject != null)
//                {
//                    GameObject point = spawnPositions.transform.GetChild(i).gameObject;
//                    dists[i] = Vector3.Distance(point.transform.position, detectedObject.worldPos);
//                    if (dists[i] < Distans)
//                    {
//                        point.GetComponent<Renderer>().material.color = Color.cyan;
//                    }
//                    else
//                    {
//                        point.GetComponent<Renderer>().material.color = GameUtils.nameToColor[colors[i]];
//                    }
//                }
//            }
//            if (dists[0] < Distans && dists[1] < Distans && dists[2] < Distans && dists[3] < Distans)
//            {
//                DestroySpawnPositions();
//                taskComplet = true;
//            }
//        }

//    }

//    public void NewTable()
//    {
//        DestroySpawnPositions();
//        taskComplet = true;
//    }

//    private void DestroySpawnPositions()
//    {
//        foreach (Transform item in spawnPositions.transform)
//        {
//            Destroy(item.gameObject);
//        }
//    }

//    private void ResetBricksInFrame()
//    {
//        bricksInFrame = new Dictionary<string, int> { { "red", 0 }, { "green", 0 }, { "blue", 0 }, { "yellow", 0 }, { "magenta", 0 } };
//    }
//}
