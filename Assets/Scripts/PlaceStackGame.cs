using Meta.XR.MRUtilityKit;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using static Unity.VisualScripting.Antlr3.Runtime.Tree.TreeWizard;

public class PlaceStackGame : MonoBehaviour
{
    public ObjectDetector objectDetection;
    public GameObject spawnPositions;
    public Transform centerCam;
    public GameObject canvas;
    public GameObject cubeParent;

    private FindSpawnPositions _findSpawnPositions;
    private Transform[] spawnPoints;

    public int maxStackSize = 4;
    public float distThreshold = 0.08f;
    public float stackThreshold = 0.06f;

    bool taskComplet = false;

    Dictionary<string, int> bricksInFrame = new Dictionary<string, int>();
    Dictionary<string, int> briksToBuildStack = new Dictionary<string, int> { { "red", 1 }, { "green", 2 }, { "blue", 2 }, { "yellow", 3 }, { "magenta", 0 } };
    int numberOfBricksInGame = 0;
    List<string> masterStack = new List<string>();
    List<List<string>> stacksToBuild = new();

    string state = "setup";
    string[] objectsToDetect = { "red", "green", "blue", "yellow" };

    List<GameObject> drawnBricks = new List<GameObject>();

    float[] dists;
    bool[] complted;

    List<DetectedObject> bricks = new List<DetectedObject>();
   
    MRUKRoom room;
    List<MRUKAnchor> anchors = new();

    Vector3 anchorPoint = new Vector3(0, 0, 0);
    Vector3 offsetDir = new Vector3(0, 0, 0);
    Vector3 displayPos = new Vector3();
    Vector3 displayOfset = new Vector3(0, 0, -0.05f);

    bool runOnce = true;

    List<List<string>> debugStatToBuild = new List<List<string>>
    {
        new List<string>{ "red", "green", "yellow" },
        new List<string>{ "yellow" },
        new List<string>{ "green","blue" },
        new List<string>{ "blue", "yellow" },
    };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        numberOfBricksInGame = briksToBuildStack.Values.Sum();
        _findSpawnPositions = spawnPositions.GetComponent<FindSpawnPositions>();
        _findSpawnPositions.SpawnAmount = numberOfBricksInGame;
        objectDetection.OnObjectsDetected += HandleBricksDetected;

    }

    private void HandleBricksDetected(object sender, ObjectDetectedEventArgs e)
    {
        bricks = new List<DetectedObject>();
        e.DetectedObjects.ForEach(brick =>
        {
            if (objectsToDetect.Contains(brick.labelName))
            {
                bricks.Add(brick);
            }
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (runOnce)
        {
            GetRoom();
        }

        if (state == "setup")
        {
            Setup();
        }
        else if (state == "Seprate")
        {
            Seprate();
        }
        else if (state == "play")
        {
            Play();
        }

    }

    private void Setup()
    {
        ClearText();
        ResetBricksInFrame();
        DestroySpawnPositions();

        drawnBricks.ForEach(Destroy);
        drawnBricks.Clear();

        bricksInFrame = GetBricksInFrame(bricks, true);
        GameUtils.AddText(centerCam, canvas, "Setup", displayPos + new Vector3(0,0.1f,0), Color.white, 3);
        if ((bricksInFrame["red"] == 1 && bricksInFrame["green"] == 2 && bricksInFrame["blue"] == 2 && bricksInFrame["yellow"] == 3))// || bricks.Count == 4)
        {
            drawnBricks.ForEach(Destroy);
            drawnBricks.Clear();
            taskComplet = true;
            state = "play";
        }
    }

    private void Seprate()
    {
        ClearText();
        GameUtils.AddText(centerCam, canvas, "Seprate", displayPos + new Vector3(0, 0.1f, 0), Color.white, 3);
        DestroyCubes(0);
        DestroyCubes(1);

        List<List<DetectedObject>> stacksInFrame = FindStacksInFrame(bricks);

        if (stacksInFrame.Count == numberOfBricksInGame)
        {
            NewTable();
            taskComplet = false;
            state = "play";
        }
    }

    private void Play()
    {
        ClearText();
        GameUtils.AddText(centerCam, canvas, "Play", displayPos + new Vector3(0, 0.1f, 0), Color.white, 3);
        DestroyCubes(1);

        if (taskComplet)
        {
            DestroyCubes(0);
            NewTable();
            //state = "seprate";
        }
        else
        {
            List<List<DetectedObject>> stacksInFrame = FindStacksInFrame(bricks);
            float[,] distMat = GameUtils.PointsStackDistansMat(stacksInFrame, spawnPoints);
            List<int> ints = GameUtils.ClosestStacks(distMat);

            DrawStacks(stacksInFrame, distMat, ints);

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (distMat[i, ints[i]] < distThreshold)
                {
                    List<string> placedStack = GameUtils.DetectedObjectListToStringList(stacksInFrame[ints[i]]);

                    if (GameUtils.HaveSameElementAtSameIndex(stacksToBuild[i], placedStack) || complted[i])
                    {
                        spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = Color.green;
                        complted[i] = true;
                    }
                    else
                    {
                        spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = Color.white;
                    }
                }
            }
            taskComplet = CheckIfAllDone();
        }
    }

    private void DrawStacks(List<List<DetectedObject>> stacksInFrame, float[,] distMat, List<int> ints)
    {
        for (int i = 0; i < stacksInFrame.Count; i++)
        {
            GameObject cube = stacksInFrame[i][0].DrawSmall();
            cube.transform.parent = cubeParent.transform.GetChild(1);
            cube.GetComponent<Renderer>().material.color = Color.gray;
           //  GameUtils.AddText(centerCam, canvas, "Id: "+ i + "\n"+distMat[i, ints[i]]+"", cube.transform.position + new Vector3(0, 0.1f, 0), Color.gray, 1.5f);
        }
    }

    private List<List<DetectedObject>> FindStacksInFrame(List<DetectedObject> detectedBricks)
    {
        float[,] distArr = new float[1, 1];
        if (detectedBricks.Count > 1)
        {
            distArr = GameUtils.DistMat(detectedBricks);
        }

        int[,] ids = new int[detectedBricks.Count, 2];
        ids = GameUtils.closestBricks(distArr, stackThreshold);

        List<List<int>> stacks = GameUtils.FindConnectedComponents(ids);
        List<List<DetectedObject>> stacksColor = new List<List<DetectedObject>>();
        
        Vector3 pos = displayPos + offsetDir * 0.2f;
        Vector3 offset = new Vector3(0, 0, 0);
        foreach (var stack in stacks)
        {
            List<DetectedObject> stackColorRow = new List<DetectedObject>();
            if (stack.Count > 2)
            {
                float y1 = detectedBricks[stack[0]].worldPos.y;
                float y2 = detectedBricks[stack[stack.Count - 1]].worldPos.y;
                if (y2 < y1)
                {
                    stack.Reverse();
                }
            }

            stack.Sort((a, b) => detectedBricks[a].worldPos.y.CompareTo(detectedBricks[b].worldPos.y));
         
            //Debug display
            for (int i = 0; i < stack.Count; i++)
            {
                GameObject cube = Instantiate(GameManager.Instance.brickPrefab, pos + offset + new Vector3(0, 0.03f, 0) * i, Quaternion.identity, cubeParent.transform.GetChild(1));
                cube.GetComponent<Renderer>().material.color = GameUtils.nameToColor[detectedBricks[stack[i]].labelName];
                stackColorRow.Add(detectedBricks[stack[i]]);
            }
            offset += new Vector3(0.05f, 0, 0);
            stacksColor.Add(stackColorRow);
        }
        return stacksColor;
    }


    public void NewTable()
    {
        DestroySpawnPositions();
        if (spawnPositions.transform.childCount == 0)
        {
            while (masterStack.Count < numberOfBricksInGame)
            {
                masterStack = GameUtils.GenetateStack(briksToBuildStack);
            }
            stacksToBuild = GameUtils.SplitStackRandomly(masterStack, maxStackSize);

            spawnPoints = GetSpawnPoints(stacksToBuild.Count);

            for (int i = 0; i < stacksToBuild.Count; i++)
            {
                List<GameObject> tempStack = GameUtils.DrawStack(stacksToBuild[i], spawnPoints[i].position + offsetDir * 0.04f);
                foreach (GameObject item in tempStack)
                {
                    item.transform.parent = cubeParent.transform.GetChild(0);
                }
                GameObject cirkel = GameUtils.MakeInteractionCirkle(spawnPoints[i].position + new Vector3(0, -0.03f, 0), Color.white);
                cirkel.transform.localScale = new Vector3(0.08f, 0.001f, 0.08f);
                cirkel.transform.parent = spawnPoints[i];
            }

            dists = new float[stacksToBuild.Count];
            complted = new bool[stacksToBuild.Count];
            for (int i = 0; i < dists.Length; i++)
            {
                dists[i] = 100;
                complted[i] = false;
            }
            taskComplet = false;
        }
        
    }

    private bool CheckIfAllDone()
    {
        foreach (bool b in complted)
        {
            if (!b)
            {
                return false;
            }
        }
        return true;
    }

    private Transform[] GetSpawnPoints(int numberOfPoints)
    {
        _findSpawnPositions.SpawnAmount = numberOfPoints;
        _findSpawnPositions.StartSpawn();

        Transform[] points = new Transform[numberOfPoints];

        for (int i = 0; i < spawnPositions.transform.childCount; i++)
        {
            points[i] = spawnPositions.transform.GetChild(i);
        }

        return points;
    }

    private void ClearText()
    {
        foreach (Transform t in canvas.transform)
        {
            Destroy(t.gameObject);
        }
    }

    private void DestroySpawnPositions()
    {
        foreach (Transform item in spawnPositions.transform)
        {
            Destroy(item.gameObject);
        }
    }

    private void DestroyCubes(int i)
    {
        foreach (Transform item in cubeParent.transform.GetChild(i))
        {
            Destroy(item.gameObject);
        }
    }

    private void ResetBricksInFrame()
    {
        bricksInFrame = new Dictionary<string, int> { { "red", 0 }, { "green", 0 }, { "blue", 0 }, { "yellow", 0 }, { "magenta", 0 } };
    }

    private Dictionary<string, int> GetBricksInFrame(List<DetectedObject> bricks, bool draw = false)
    {
        Dictionary<string, int> res = new Dictionary<string, int> { { "red", 0 }, { "green", 0 }, { "blue", 0 }, { "yellow", 0 }, { "magenta", 0 } };
        foreach (var brick in bricks)
        {
            res[brick.labelName]++;

            if (true)
            {
                GameObject cube = brick.DrawSmall();
                drawnBricks.Add(cube);
            }
        }
        return res;
    }

    private void GetRoom()
    {
        room = MRUK.Instance.GetCurrentRoom();
        Debug.LogWarning(room != null);
        if (room != null)
        {
            anchors = room.Anchors;
            foreach (MRUKAnchor anchor in anchors)
            {
                if (anchor.Label == MRUKAnchor.SceneLabels.TABLE)
                {
                    anchorPoint = anchor.gameObject.transform.position;
                    // if (anchor.PlaneRect.HasValue)
                    // {
                    //     var tablePlane = anchor.PlaneRect.Value;
                    //
                    //     foreach (var point in DiskSampling.GenerateDiskSamples(tablePlane,5,50,10))
                    //     {
                    //         var newObject = Instantiate(GameManager.Instance.brickPrefab, anchor.transform);
                    //
                    //         newObject.transform.localPosition = point;
                    //
                    //         newObject.transform.parent = null;
                    //     }
                    // }
                    
                    
                    // for (int i = 0; i < 100; i++)
                    // {
                    //     
                    // }
                    // displayPos.rotation = Quaternion.Euler(anchor.gameObject.transform.localRotation.eulerAngles + new Vector3(-90, 0, -180));
                }
            }
            runOnce = false;
        }

        if (!runOnce)
        {
            offsetDir = (anchorPoint - new Vector3(centerCam.position.x, anchorPoint.y, centerCam.position.z)).normalized;

            displayPos = anchorPoint + offsetDir * 0.2f;

            displayOfset = offsetDir * -0.05f;

        }
    }
}

