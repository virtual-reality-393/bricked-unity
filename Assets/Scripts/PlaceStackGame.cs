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
    public int minStackSize = 2;
    public SliceMethod sliceMethod = SliceMethod.Max;
    public float distThreshold = 0.08f;
    public float stackThreshold = 0.06f;

    bool taskComplet = false;

    Dictionary<string, int> bricksInFrame = new Dictionary<string, int>();
    Dictionary<string, int> briksToBuildStack = new Dictionary<string, int> { { "red", 1 }, { "green", 2 }, { "blue", 2 }, { "yellow", 3 }, { "magenta", 0 } };
    int numberOfBricksInGame = 0;
    List<string> masterStack = new List<string>();
    List<List<string>> stacksToBuild = new();

    private GameState state = GameState.Setup;
    string[] objectsToDetect = { "red", "green", "blue", "yellow" };

    List<GameObject> drawnBricks = new List<GameObject>();
    List<GameObject> adminMenuVisuals = new List<GameObject>();

    float[] dists;
    bool[] complted;

    List<DetectedObject> bricks = new List<DetectedObject>();
   
    MRUKRoom room;
    List<MRUKAnchor> anchors = new();
    MRUKAnchor tableAnchor = null;
    Rect ourPlaneRect = new Rect(0, 0, 0, 0);
    GameObject rectPos1;
    GameObject rectPos2;

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

    private bool fixStack = false; // Pls fix the gameplay loop :|

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
        fixStack = true;
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
            else if(brick.labelName == "sheep" && debugMode)
            {
                rectPos1.transform.position = brick.worldPos;
                GameObject sheep = brick.DrawSmall();
                adminMenuVisuals.Add(sheep);
            }
            else if(brick.labelName == "pig" && debugMode)
            {
                rectPos2.transform.position = brick.worldPos;
                GameObject pig = brick.DrawSmall();
                adminMenuVisuals.Add(pig);
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
            DisplayRect();
        }

        if (state == GameState.Setup && !runOnce)
        {
            Setup();
        }
        else if (state == GameState.Separate)
        {
            Seprate();
        }
        else if (state == GameState.Play)
        {
            Play();
        }
    }

    private void DisplayRect()
    {
        Rect rect = ourPlaneRect;
        GameObject pos1 = GameUtils.MakeInteractionCirkle(tableAnchor.transform.position + new Vector3(rect.xMin, 0, rect.yMin), Color.magenta);
        GameObject pos2 = GameUtils.MakeInteractionCirkle(tableAnchor.transform.position + new Vector3(rect.xMin, 0, rect.yMax), Color.magenta);
        GameObject pos3 = GameUtils.MakeInteractionCirkle(tableAnchor.transform.position + new Vector3(rect.xMax, 0, rect.yMin), Color.magenta);
        GameObject pos4 = GameUtils.MakeInteractionCirkle(tableAnchor.transform.position + new Vector3(rect.xMax, 0, rect.yMax), Color.magenta);
        adminMenuVisuals.Add(pos1);
        adminMenuVisuals.Add(pos2);
        adminMenuVisuals.Add(pos3);
        adminMenuVisuals.Add(pos4);
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
            state = GameState.Play;
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
            state = GameState.Play;
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
            if (fixStack)
            {
                
                fixStack = true;
                // Pls fix the gameplay loop >:(
            }
            stacksInFrame = FindStacksInFrame(FixStacks(stacksInFrame, bricks));

            if (debugMode)
            {
                DrawDebugStacks(stacksInFrame);
            }
            
            float[,] distMat = GameUtils.PointsStackDistansMat(stacksInFrame, spawnPoints);
            List<int> ints = GameUtils.ClosestStacks(distMat);

            if (debugMode)
            {
                DrawStackPositions(stacksInFrame, distMat, ints);
            }

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (distMat[i, ints[i]] < distThreshold)
                {
                    List<string> placedStack = GameUtils.DetectedObjectListToStringList(stacksInFrame[ints[i]]);

                    if (GameUtils.HaveSameElementAtSameIndex(stacksToBuild[i], placedStack) || complted[i])
                    {
                        var col = Color.green;
                        col.a = 0.5f;
                        spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = col;
                        complted[i] = true;
                    }
                    else
                    {
                        var col = Color.white;
                        col.a = 0.5f;
                        spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = col;
                    }
                }
            }
            taskComplet = CheckIfAllDone();
        }
    }

    private List<DetectedObject> FixStacks(List<List<DetectedObject>> stacksInFrame, List<DetectedObject> detectedBricks)
    {
        var res = new List<DetectedObject>();
        foreach(var stack in stacksInFrame)
        {
            var tempHeight = stack[^1].worldPos.y-displayPos.y;

            var tempStack = Instantiate(GameManager.Instance.stackPrefab, stack[^1].worldPos-new Vector3(0,tempHeight/2,0), Quaternion.identity);
            tempStack.transform.localScale = new Vector3(tempStack.transform.localScale.x*2, tempHeight+0.1f,
                tempStack.transform.localScale.z);
            tempStack.transform.forward = offsetDir;
            tempStack.transform.parent = cubeParent.transform.GetChild(1);
            // tempStack.GetComponent<Renderer>().enabled = debugMode;
        }
        
        

        foreach (var brick in detectedBricks)
        {
            if(Physics.Raycast(centerCam.transform.position, (brick.worldPos - centerCam.transform.position + Vector3.up*0.02f).normalized,out RaycastHit hit))
            {
                res.Add(new DetectedObject(brick.labelIdx,brick.labelName,hit.point));
            }
        }


        return res;
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

        GameObject addMax = GameUtils.MakeInteractionCirkle(menuPos - offset*4, Color.blue);
        adminMenuVisuals.Add(addMax);
        GameUtils.AddText(centerCam, canvas, "+1 to max size \nCurrent max size: " + maxStackSize, menuPos - offset * 4 + new Vector3(0, 0.01f, 0), Color.white, 0.8f);

        GameObject removeMax = GameUtils.MakeInteractionCirkle(menuPos - offset*3, Color.blue);
        adminMenuVisuals.Add(removeMax);
        GameUtils.AddText(centerCam, canvas, "-1 from max size \nCurrent max size: " + maxStackSize, menuPos - offset * 3 + new Vector3(0, 0.01f, 0), Color.white, 0.8f);

        GameObject addMin = GameUtils.MakeInteractionCirkle(menuPos - offset * 2, Color.blue);
        adminMenuVisuals.Add(addMin);
        GameUtils.AddText(centerCam, canvas, "+1 to min size \nCurrent min size: " + minStackSize, menuPos - offset * 2 + new Vector3(0, 0.01f, 0), Color.white, 0.8f);

        GameObject removeMin = GameUtils.MakeInteractionCirkle(menuPos - offset * 1, Color.blue);
        adminMenuVisuals.Add(removeMin);
        GameUtils.AddText(centerCam, canvas, "-1 from min size \nCurrent min size: " + minStackSize, menuPos - offset * 1 + new Vector3(0, 0.01f, 0), Color.white, 0.8f);

        GameObject newSilceMode = GameUtils.MakeInteractionCirkle(menuPos + offset * 0, Color.blue);
        adminMenuVisuals.Add(newSilceMode);
        GameUtils.AddText(centerCam, canvas, "Change slice mode\nCurrent: " + sliceMethod.ToString(), menuPos + offset * 0 + new Vector3(0, 0.01f, 0), Color.white, 0.8f);

        GameObject completTask = GameUtils.MakeInteractionCirkle(menuPos + offset * 1, Color.blue);
        adminMenuVisuals.Add(completTask);
        GameUtils.AddText(centerCam, canvas, "Complet task", menuPos + offset * 1 + new Vector3(0, 0.01f, 0), Color.white, 0.8f);

        GameObject toSetup = GameUtils.MakeInteractionCirkle(menuPos + offset * 2, Color.blue);
        adminMenuVisuals.Add(toSetup);
        GameUtils.AddText(centerCam, canvas, "To setup", menuPos + offset * 2 + new Vector3(0, 0.01f, 0), Color.white, 0.8f);

        GameObject makeNewRect = GameUtils.MakeInteractionCirkle(menuPos + offset * 3, Color.blue);
        adminMenuVisuals.Add(makeNewRect);
        GameUtils.AddText(centerCam, canvas, "Make new rect", menuPos + offset * 3 + new Vector3(0, 0.01f, 0), Color.white, 0.8f);

        GameObject resteRect = GameUtils.MakeInteractionCirkle(menuPos + offset * 4, Color.blue);
        adminMenuVisuals.Add(resteRect);
        GameUtils.AddText(centerCam, canvas, "Reset rect", menuPos + offset * 4 + new Vector3(0, 0.01f, 0), Color.white, 0.8f);

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
                if (maxStackSize - 1 >= minStackSize)
                {
                    maxStackSize--;
                }
                break;

            case 3:
                if (minStackSize + 1 <= maxStackSize)
                {
                    minStackSize++;
                }
                break;

            case 4:
                if (minStackSize - 1 >= 1)
                {
                    minStackSize--;
                }
                break;

            case 5:
                if (sliceMethod == SliceMethod.Max)
                {
                    sliceMethod = SliceMethod.Min;
                }
                else if (sliceMethod == SliceMethod.Min)
                {
                    sliceMethod = SliceMethod.Random;
                }
                else if (sliceMethod == SliceMethod.Random)
                {
                    sliceMethod = SliceMethod.Equalize;
                }
                else if (sliceMethod == SliceMethod.Equalize)
                {
                    sliceMethod = SliceMethod.Even;
                }
                else
                {
                    sliceMethod = SliceMethod.Max;
                }
                break;

            case 6:
                taskComplet = true;
                break;

            case 7:
                state = GameState.Setup;
                DestroyCubes(0);
                DestroyCubes(1);
                break;

            case 8:
                float minX = Mathf.Min(rectPos1.transform.localPosition.x, rectPos2.transform.localPosition.x);
                float maxX = Mathf.Max(rectPos1.transform.localPosition.x, rectPos2.transform.localPosition.x);
                float minY = Mathf.Min(rectPos1.transform.localPosition.y, rectPos2.transform.localPosition.y);
                float maxY = Mathf.Max(rectPos1.transform.localPosition.y, rectPos2.transform.localPosition.y);
                ourPlaneRect = new Rect(minX,minY,maxX-minX,maxY-minY);
                Debug.LogWarning("Rect: " + ourPlaneRect);
                break;


            case 9:
                ourPlaneRect = (Rect)tableAnchor.PlaneRect;
                break;

            default:
                break;
        }
        adminAction = -1;

    }

    private void DrawStackPositions(List<List<DetectedObject>> stacksInFrame, float[,] distMat, List<int> ints)
    {
        for (int i = 0; i < stacksInFrame.Count; i++)
        {
            GameObject cube = stacksInFrame[i][0].DrawSmall();
            cube.transform.parent = cubeParent.transform.GetChild(1);
            cube.GetComponent<Renderer>().material.color = Color.gray;
            //  GameUtils.AddText(centerCam, canvas, "Id: "+ i + "\n"+distMat[i, ints[i]]+"", cube.transform.position + new Vector3(0, 0.1f, 0), Color.gray, 1.5f);
        }
    }

    private void DrawDebugStacks(List<List<DetectedObject>> stacksInFrame)
    {
        Vector3 pos = displayPos + offsetDir * 0.2f;
        Vector3 offset = new Vector3(0, 0, 0);

        foreach (var stack in stacksInFrame)
        {
            for (int i = 0; i < stack.Count; i++)
            {
                GameObject cube = Instantiate(GameManager.Instance.brickPrefab, pos + offset + new Vector3(0, 0.03f, 0) * i, Quaternion.identity, cubeParent.transform.GetChild(1));
                cube.GetComponent<Renderer>().material.color = GameUtils.nameToColor[stack[i].labelName];
            }

            offset += new Vector3(0.05f, 0, 0);
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
        
        
        foreach (var stack in stacks)
        {
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

            var stackColorRow = stack.Select(t => detectedBricks[t]).ToList();

            
            stacksColor.Add(stackColorRow);
        }
        return stacksColor;
    }


    public void NewTable()
    {
        DestroySpawnPositions();
        if (spawnPositions.transform.childCount == 0)
        {
            stacksToBuild = StackGenerator.GenerateStacks(briksToBuildStack, minStackSize, maxStackSize, sliceMethod);

            spawnPoints = GameUtils.DiskSampledSpawnPoints(tableAnchor, stacksToBuild.Count, spawnPositions.transform, ourPlaneRect);
            //if (ourPlaneRect == new Rect(0,0,0,0))
            //{
            //    spawnPoints = GameUtils.DiskSampledSpawnPoints(tableAnchor, stacksToBuild.Count, spawnPositions.transform);
            //}
            //else
            //{
               
            //}

            if (spawnPoints == null)
            {
                GameUtils.AddText(centerCam, canvas, "No spawnPoints", tableAnchor.transform.position, Color.white);
            }


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

            ourPlaneRect = (Rect)tableAnchor.PlaneRect;

            rectPos1 = new GameObject("rectPos1");
            rectPos1.transform.parent = tableAnchor.transform;

            rectPos2 = new GameObject("rectPos2");
            rectPos2.transform.parent = tableAnchor.transform;
        }
    }
}


public enum GameState
{
    Setup,
    Play,
    Separate
}

