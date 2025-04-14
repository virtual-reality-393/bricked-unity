using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using System.Linq;

public class StackingGame : MonoBehaviour
{
    public ObjectDetector objectDetection;
    public GameObject spawnPositions;
    public GameObject cubeParent;
    
    public Transform centerCam;
    
    private FindSpawnPositions _findSpawnPositions;

    public GameObject canvas;

    public float distans = 0.05f;
    public float threshold = 0.06f;

    bool taskComplet = true;
    bool makeNewStack = true;

    Dictionary<string, int> bricksInFrame = new Dictionary<string, int>();
    Dictionary<string, int> briksToBuildStack = new Dictionary<string, int> { { "red", 1 }, { "green", 2 }, { "blue", 2 }, { "yellow", 3 }, { "magenta", 0 } };

    List<string> stackToBuild = new List<string>();

    string state = "setup";

    List<GameObject> drawnBricks = new List<GameObject>();

    MRUKRoom room;
    List<MRUKAnchor> anchors = new();

    Vector3 displayPos = new Vector3();
    Vector3 debugDisplayPos = new Vector3();

    private bool runOnce = true;

    private int stackHeight = 2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _findSpawnPositions = spawnPositions.GetComponent<FindSpawnPositions>();
        //displayPos = new GameObject().transform;
        //DebugDisplayPos = new GameObject().transform;
    }
        

    // Update is called once per frame
    void Update()
    {
        if (runOnce)
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
                        displayPos = anchor.gameObject.transform.position + new Vector3(0,0,0.2f);
                       // displayPos.rotation = Quaternion.Euler(anchor.gameObject.transform.localRotation.eulerAngles + new Vector3(-90, 0, -180));
                    }
                }
                runOnce = false;
            }

            if (!runOnce)
            {
                debugDisplayPos = displayPos + new Vector3(0.2f, 0, 0);
                //Brick b = new Brick("red", displayPos);
                //b.Draw(Color.magenta);
                //Brick b2 = new Brick("green", DebugDisplayPos);
                //b2.Draw(Color.cyan);
            }
            
        }

        if (state == "setup")
        {
            Setup();
        }
        else if (state == "play")
        {
            Play();
        }
    }


    private void Setup()
    {
        List<DetectedObject> bricks = objectDetection.GetBricks();
        ResetBricksInFrame();
        DestroySpawnPositions();

        drawnBricks.ForEach(Destroy);
        drawnBricks.Clear();

        foreach (Transform t in canvas.transform)
        {
            Destroy(t.gameObject);
        }

        foreach (var brick in bricks)
        {
            bricksInFrame[brick.labelName]++;
            GameObject cube = brick.DrawSmall();
            GameUtils.AddText(centerCam, canvas, brick.worldPos.ToString(), brick.worldPos, GameUtils.nameToColor[brick.labelName]);
            drawnBricks.Add(cube);

        }

        if ((bricksInFrame["red"] == 1 && bricksInFrame["green"] == 2 && bricksInFrame["blue"] == 2 && bricksInFrame["yellow"] == 3))// || bricks.Count == 4)
        {
            drawnBricks.ForEach(Destroy);
            drawnBricks.Clear();
            state = "play";
        }
    }

    private void Play()
    {
        List<DetectedObject> bricks = objectDetection.GetBricks();
        ResetBricksInFrame();
        drawnBricks.ForEach(Destroy);
        drawnBricks.Clear();
        foreach (Transform t in canvas.transform)
        {
            Destroy(t.gameObject);
        }

        foreach (var brick in bricks)
        {
            bricksInFrame[brick.labelName]++;
            GameObject cube = brick.DrawSmall();
            GameUtils.AddText(centerCam, canvas, brick.worldPos.ToString(), brick.worldPos, GameUtils.nameToColor[brick.labelName]);
            drawnBricks.Add(cube);

        }
        DestroyCubes(1);

        //If the task is completed, choose new colors
        if (taskComplet && makeNewStack)
        {
            //Make stack to build and place it on table
            stackToBuild = GameUtils.GenetateStack(briksToBuildStack);
            _findSpawnPositions.StartSpawn();
            //Vector3 pos = spawnPositions.transform.GetChild(0).transform.position;
            stackHeight = 2;
            DisplayStackToBuild(stackHeight);

            taskComplet = false;
            makeNewStack = false;
        }
        else
        {
            GameUtils.AddText(centerCam, canvas, "Debug info", debugDisplayPos + new Vector3(0, 0.03f, 0), Color.white, 2);
            if (taskComplet)
            {
                GameUtils.AddText(centerCam, canvas, "Seprate bricks", displayPos + new Vector3(0, 0.03f, 0), Color.white, 2);
            }
            else
            {
                GameUtils.AddText(centerCam, canvas, "Stack to build", displayPos + new Vector3(0, 0.03f, 0) * stackHeight, Color.white, 2);
            }

            //Tjeck stacks in frame    

            float[,] distArr = new float[1,1];
            if (bricks.Count > 1)
            {
                distArr = DistMat(bricks);
            }

            int[,] ids = new int[bricks.Count, 2];
            ids = closestBricks(distArr);

            List<List<int>> stacks = FindConnectedComponents(ids);
            List<List<string>> stacksColor = new List<List<string>>();
            Vector3 pos = debugDisplayPos; // spawnPositions.transform.GetChild(1).transform.position;
            Vector3 offset = new Vector3(0, 0, 0);
            foreach (var stack in stacks)
            {
                List<string> stackColorRow = new List<string>();
                if (stack.Count > 2)
                {
                    float y1 = bricks[stack[0]].worldPos.y;
                    float y2 = bricks[stack[stack.Count - 1]].worldPos.y;
                    if (y2 < y1)
                    {
                        stack.Reverse();
                    }
                }

                stack.Sort((a, b) => bricks[a].worldPos.y.CompareTo(bricks[b].worldPos.y));
                for (int i = 0; i < stack.Count; i++)
                {
                    GameObject cube = Instantiate(GameManager.Instance.brickPrefab, pos + offset + new Vector3(0, 0.03f, 0) * i, Quaternion.identity, cubeParent.transform.GetChild(1));
                    cube.GetComponent<Renderer>().material.color = GameUtils.nameToColor[bricks[stack[i]].labelName];
                    stackColorRow.Add(bricks[stack[i]].labelName);
                }
                offset += new Vector3(0.05f, 0, 0);
                stacksColor.Add(stackColorRow);
            }



            bool b = false;
            foreach (var stack in stacksColor)
            {
                if (stackHeight == stack.Count)
                {
                    b = true;
                    for (int i = 0; i < stackHeight; i++)
                    {
                        b = b && stackToBuild[i] == stack[i];
                        if (!b)
                        {
                            break;
                        }
                    }
                }
            }


            if (b)
            {
                stackHeight++;
                DisplayStackToBuild(stackHeight);
            }

            if (stackHeight > stackToBuild.Count)
            {
                taskComplet = true;
                DestroyCubes(0);
            }

            int numBricks = GameUtils.GenerateListFromDict(briksToBuildStack).Count;
            if (taskComplet && stacks.Count == numBricks)
            {
                makeNewStack = true;
                DestroySpawnPositions();
            }

        }
    }

    private void DisplayStackToBuild(int stackHight)
    {
        DestroyCubes(0);
        for (int i = 0; i < stackHight; i++)
        {
            GameObject cube = Instantiate(GameManager.Instance.brickPrefab, displayPos + new Vector3(0, 0.03f, 0) * i, Quaternion.identity, cubeParent.transform.GetChild(0));
            cube.GetComponent<Renderer>().material.color = GameUtils.nameToColor[stackToBuild[i]];
        }
    }

    public List<string> SortGameObjectsByY(List<DetectedObject> objects)
    {
        // Sort the list using a custom comparison based on the Y-coordinate
        objects.Sort((obj1, obj2) => obj1.worldPos.y.CompareTo(obj2.worldPos.y));

        List<string> res = new List<string>();
        foreach (DetectedObject b in objects)
        {
            res.Add(b.labelName);
        }

        return res;
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


    int[] GetTwoLowestIndices(float[] arr)
    {
        return arr
            .Select((value, index) => new { Value = value, Index = index }) // Pair values with indices
            .OrderBy(x => x.Value) // Sort by value
            .Take(2) // Take the two lowest
            .Select(x => x.Index) // Extract indices
            .ToArray();
    }

    private int[,] closestBricks(float[,] distArr)
    {
        int[,] ids = new int[distArr.GetLength(0), 2];
        for (int i = 0; i < distArr.GetLength(0); i++)
        {
            float[] row = new float[distArr.GetLength(1)];
            for (int j = 0; j < distArr.GetLength(1); j++)
            {
                row[j] = distArr[i, j];
            }

            int[] rowId = GetTwoLowestIndices(row);
            ids[i, 0] = distArr[i, rowId[0]] < threshold ? rowId[0] : -1;
            ids[i, 1] = distArr[i, rowId[1]] < threshold ? rowId[1] : -1;
        }
        return ids;
    }

    private float[,] DistMat(List<DetectedObject> bricks)
    {
        float[,] distArr = new float[bricks.Count, bricks.Count];
        for (int i = 0; i < bricks.Count; i++)
        {
            for (int j = 0; j < bricks.Count; j++)
            {
                if (i == j)
                {
                    distArr[i, j] = 10000;
                }
                else
                {
                    distArr[i, j] = Vector3.Distance(bricks[i].worldPos, bricks[j].worldPos);
                }
            }
        }
        return distArr;
    }


    List<List<int>> FindConnectedComponents(int[,] graph)
    {
        int n = graph.GetLength(0);
        List<List<int>> components = new List<List<int>>();
        HashSet<int> visited = new HashSet<int>();

        for (int i = 0; i < n; i++)
        {
            if (!visited.Contains(i))
            {
                List<int> component = new List<int>();
                DFS(graph, i, visited, component);
                components.Add(component);
            }
        }

        return components;
    }

    void DFS(int[,] graph, int node, HashSet<int> visited, List<int> component)
    {
        Stack<int> stack = new Stack<int>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            int curr = stack.Pop();
            if (!visited.Contains(curr))
            {
                visited.Add(curr);
                component.Add(curr);

                // Add connected nodes to stack
                for (int j = 0; j < 2; j++)
                {
                    int neighbor = graph[curr, j];
                    if (neighbor != -1 && !visited.Contains(neighbor))
                    {
                        stack.Push(neighbor);
                    }
                }
            }
        }
    }
}
