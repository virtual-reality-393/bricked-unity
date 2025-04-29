using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

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
    List<GameObject> adminMenuVisuals = new List<GameObject>();

    float[] dists;
    bool[] complted;

    List<DetectedObject> bricks = new List<DetectedObject>();
   
    MRUKRoom room;
    List<MRUKAnchor> anchors = new();
    MRUKAnchor tableAnchor = null;

    Vector3 anchorPoint = new Vector3(0, 0, 0);
    Vector3 offsetDir = new Vector3(0, 0, 0);
    Vector3 displayPos = new Vector3();
    Vector3 displayOfset = new Vector3(0, 0, -0.05f);

    Vector3 smallPenguinPos = new Vector3(10, 10, 10);

    bool runOnce = true;
    bool debugMode = false;
    bool canDoAdminInteraction = true;

    string setupText = "To start find the needed bricks.";
    string seprateText = "Seprate";
    string playText = "Build the displayed stacks and place them in the cirkel.";

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
            else if (brick.labelName == "small penguin")
            {
                smallPenguinPos = brick.worldPos;
                debugMode = true;
            }
            else if (brick.labelName == "big penguin")
            {
                debugMode = false;
                smallPenguinPos = new Vector3(10, 10, 10);
            }
        });
    }

    // Update is called once per frame
    void Update()
    {
        ClearText();
        if (runOnce)
        {
            GetRoom();
        }

        adminMenuVisuals.ForEach(Destroy);
        adminMenuVisuals.Clear();
        if (debugMode)
        {
            DebugMenu();
        }

        if (state == "setup" && !runOnce)
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
        ResetBricksInFrame();
        DestroySpawnPositions();

        drawnBricks.ForEach(Destroy);
        drawnBricks.Clear();

        bricksInFrame = GetBricksInFrame(bricks, debugMode);
        GameUtils.AddText(centerCam, canvas, setupText, displayPos + new Vector3(0,0.15f,0), Color.white, 3);
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
        GameUtils.AddText(centerCam, canvas, seprateText, displayPos + new Vector3(0, 0.15f, 0), Color.white, 3);
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
        GameUtils.AddText(centerCam, canvas, playText, displayPos + new Vector3(0, 0.15f, 0), Color.white, 3);

        if (debugMode)
        {
            GameUtils.AddText(centerCam, canvas, "Debug mode \"On\". \nTo disable detect \"big penguin\".", displayPos + offsetDir * 0.18f + new Vector3(0, 0.15f, 0), Color.white, 1.5f);
        }

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

            if (debugMode)
            {
                DrawStacks(stacksInFrame, distMat, ints);
            }

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

    private void DebugMenu()
    {
        Vector3 menuPos = anchorPoint + offsetDir * -0.2f;
        Vector3 offset = new Vector3(0.07f, 0, 0);

        int adminAction = -1;

        GameObject adminpoint;
        if (!canDoAdminInteraction)
        {
            adminpoint = GameUtils.MakeInteractionCirkle(menuPos + offsetDir * 0.1f, Color.red);
            
        }
        else
        {
            adminpoint = GameUtils.MakeInteractionCirkle(menuPos + offsetDir * 0.1f, Color.green);
        }
        adminMenuVisuals.Add(adminpoint);
        GameUtils.AddText(centerCam, canvas, "" + Vector3.Distance(smallPenguinPos, adminpoint.transform.position), menuPos + offsetDir * 0.1f + new Vector3(0, 0.01f, 0), Color.white, 0.8f);

        GameObject addMax = GameUtils.MakeInteractionCirkle(menuPos + offset*0, Color.blue);
        adminMenuVisuals.Add(addMax);
        GameUtils.AddText(centerCam, canvas, "+1 to max size \nCurrent max size: " + maxStackSize, menuPos + offset * 0 + new Vector3(0, 0.01f, 0), Color.white, 0.8f);

        GameObject removeMax = GameUtils.MakeInteractionCirkle(menuPos + offset*1, Color.blue);
        adminMenuVisuals.Add(removeMax);
        GameUtils.AddText(centerCam, canvas, "-1 from max size \nCurrent max size: " + maxStackSize, menuPos + offset * 1 + new Vector3(0, 0.01f, 0), Color.white, 0.8f);

        GameObject completTask = GameUtils.MakeInteractionCirkle(menuPos + offset * 2, Color.blue);
        adminMenuVisuals.Add(completTask);
        GameUtils.AddText(centerCam, canvas, "Complet task", menuPos + offset * 2 + new Vector3(0, 0.01f, 0), Color.white, 0.8f);

        GameObject toSetup = GameUtils.MakeInteractionCirkle(menuPos + offset * 3, Color.blue);
        adminMenuVisuals.Add(toSetup);
        GameUtils.AddText(centerCam, canvas, "To setup", menuPos + offset * 3 + new Vector3(0, 0.01f, 0), Color.white, 0.8f);

        for (int i = 0; i < adminMenuVisuals.Count; i++)
        {
            float d = Vector3.Distance(smallPenguinPos, adminMenuVisuals[i].transform.position);

            if (Vector3.Distance(smallPenguinPos, adminpoint.transform.position) < 0.03)
            {
                canDoAdminInteraction = true;
            }

            if (d < 0.03 && canDoAdminInteraction)
            {
                adminAction = i;
                canDoAdminInteraction = false;
            }
        }

        switch (adminAction)
        {
            case 1:
                if (maxStackSize+1 <= briksToBuildStack.Values.Sum())
                {
                    maxStackSize++;
                }
                break;

            case 2:
                if (maxStackSize - 1 >= 1)
                {
                    maxStackSize--;
                }
                break;

            case 3:
                taskComplet = true;
                break;

            case 4:
                state = "setup";
                DestroyCubes(0);
                break;

            default:
                break;
        }
        adminAction = -1;

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

   
            for (int i = 0; i < stack.Count; i++)
            {
                //Debug display of the stacks in current frame
                if (debugMode)
                {
                    GameObject cube = Instantiate(GameManager.Instance.brickPrefab, pos + offset + new Vector3(0, 0.03f, 0) * i, Quaternion.identity, cubeParent.transform.GetChild(1));
                    cube.GetComponent<Renderer>().material.color = GameUtils.nameToColor[detectedBricks[stack[i]].labelName];
                }
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

            //spawnPoints = GetSpawnPoints(stacksToBuild.Count);
            spawnPoints = DiskSampledSpawnPoints(stacksToBuild.Count);

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

    private Transform[] DiskSampledSpawnPoints(int numberOfPoints)
    {
        Transform[] points = new Transform[numberOfPoints];
        if (tableAnchor.PlaneRect.HasValue)
        {
            var tablePlane = tableAnchor.PlaneRect.Value;

            List<Vector2> allPoints = DiskSampling.GenerateDiskSamples(tablePlane, 5, 50, 10);

            for (int i = 0; i < numberOfPoints; i++)
            {
                var point = allPoints[Random.Range(0, allPoints.Count)];

                var newObject = new GameObject();

                newObject.transform.parent = tableAnchor.transform;

                newObject.transform.position = tableAnchor.transform.position;

                newObject.transform.localPosition = point;

                newObject.transform.parent = spawnPositions.transform;

                points[i] = newObject.transform;
            }

        }

        return points;
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
                    tableAnchor = anchor;
                    anchorPoint = anchor.gameObject.transform.position;
                }
            }
            runOnce = false;
        }

        if (!runOnce)
        {
            offsetDir = (anchorPoint - new Vector3(centerCam.position.x, anchorPoint.y, centerCam.position.z)).normalized;

            displayPos = anchorPoint + offsetDir * 0.25f;

            displayOfset = offsetDir * -0.05f;

        }
    }
}

