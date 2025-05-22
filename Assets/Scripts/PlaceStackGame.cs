using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlaceStackGame : MonoBehaviour
{
    public ObjectDetector objectDetection;
    public GameObject spawnPositions;
    public Transform centerCam;
    public GameObject canvas;
    public GameObject cubeParent;

    public GameObject debugHand;

    private FindSpawnPositions _findSpawnPositions;
    private Transform[] spawnPoints;

    public int maxStackSize = 4;
    public int minStackSize = 2;
    public SliceMethod sliceMethod = SliceMethod.Random;
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
    int[] spawnpointNoDetectionCounts;
    int[] spawnpointWringStackCounts;

    List<DetectedObject> bricks = new List<DetectedObject>();
   
    MRUKRoom room;
    List<MRUKAnchor> anchors = new();
    MRUKAnchor tableAnchor = null;
    Rect ourPlaneRect = new Rect(1, 1, -1, -1);
    GameObject rectPos1;
    GameObject rectPos2;
    GameObject rectCenter;
    Vector3 planeCenter;

    Vector3 anchorPoint = new Vector3(0, 0, 0);
    Vector3 offsetDir = new Vector3(0, 0, 0);
    Vector3 displayPos = new Vector3();

    GameObject debugMenu;
    List<GameObject> debugTestObjects = new List<GameObject>();
    GameObject rectDisplay;
    Vector3 smallPenguinPos = new Vector3(10, 10, 10);

    GameObject mainText;

    bool runOnce = true;
    bool debugMode = false;
    bool canDoAdminInteraction = true;

    int levelsComplteded = 0;

    string setupText = "To start find the needed bricks.";
    string seprateText = "Separate";
    string playText = "Build the displayed stacks and place them in the circle.";

    private bool fixStack = false; // Pls fix the gameplay loop :|


    private int prevPenguin = 0;
    private int currPenguin = 0;


    List<List<string>> turtoial1 = new List<List<string>>
    {
        new List<string>{ "red"},
        new List<string>{ "blue" },
    };

    List<List<string>> turtoial2 = new List<List<string>>
    {
        new List<string>{ "red", "green", "yellow" , "blue"}
    };

    List<List<string>> debugStatToBuild = new List<List<string>>
    {
        new List<string>{ "red", "green", "yellow" },
        new List<string>{ "yellow" },
        new List<string>{ "green","blue" },
        new List<string>{ "blue", "yellow" },
    };

    // private GameObject penguinPosCircle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        numberOfBricksInGame = briksToBuildStack.Values.Sum();
        _findSpawnPositions = spawnPositions.GetComponent<FindSpawnPositions>();
        _findSpawnPositions.SpawnAmount = numberOfBricksInGame;
        objectDetection.OnObjectsDetected += HandleBricksDetected;

        rectPos1 = GameUtils.MakeInteractionCirkle(new Vector3(0, 0, 0), Color.cyan);
        rectPos1.transform.localScale = new Vector3(0.03f, 0.001f, 0.03f);

        rectPos2 = GameUtils.MakeInteractionCirkle(new Vector3(0, 0, 0), Color.cyan);
        rectPos2.transform.localScale = new Vector3(0.03f, 0.001f, 0.03f);

        rectCenter = GameUtils.MakeInteractionCirkle(new Vector3(0, 0, 0), Color.green);
        rectCenter.transform.localScale = new Vector3(0.03f, 0.001f, 0.03f);

        mainText = new GameObject("MainText");
        GameUtils.AddText(centerCam, mainText, setupText, new Vector3(0, 0, 0), Color.white, 3f);


        // penguinPosCircle = GameUtils.MakeInteractionCirkle(new Vector3(10,10,10),Color.black);

        debugMenu = new GameObject("DebugMenu");
        MakeDebugMenu(debugMenu);
        debugMenu.SetActive(debugMode);

        rectDisplay = new GameObject("RectDisplay");
        MakeRectDisplay(rectDisplay);
        rectDisplay.SetActive(debugMode);
        
        foreach (var item in debugTestObjects)
        {
            item.SetActive(debugMode);
        }

        foreach (var item in debugTestObjects)
        {
            item.SetActive(debugMode);
        }

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
                
                currPenguin += 1;
                if (currPenguin >= 3)
                {
                    debugMode = true;
                    debugMenu.SetActive(debugMode);
                    rectDisplay.SetActive(debugMode);
                    foreach (var item in debugTestObjects)
                    {
                        item.SetActive(debugMode);
                    }
                }
            }
            else if (brick.labelName == "big penguin")
            {
                debugMode = false;
                // smallPenguinPos = new Vector3(10, 10, 10);
                debugMenu.SetActive(debugMode);
                rectDisplay.SetActive(debugMode);
                foreach (var item in debugTestObjects)
                {
                    item.SetActive(debugMode);
                }
            }
            else if (brick.labelName == "sheep" && debugMode)
            {
                rectPos1.transform.position = brick.worldPos;
            }
            else if (brick.labelName == "pig" && debugMode)
            {
                rectPos2.transform.position = brick.worldPos;
            }
            else if (brick.labelName == "human" && !debugMode)
            {
                //Default settings
                maxStackSize = 4;
                minStackSize = 2;
                sliceMethod = SliceMethod.Random;
                Debug.LogError("Human detected: Slice: Random - Max Size: 4 - Min Size: 2");
            }
            else if (brick.labelName == "sheep" && !debugMode)
            {
                //One big stack
                maxStackSize = 8;
                minStackSize = 4;
                sliceMethod = SliceMethod.Max;
                Debug.LogError("Sheep detected: Slice: Max - Max Size: 8 - Min Size: 4");
            }
            else if (brick.labelName == "pig" && !debugMode)
            { 
                //many small stacks
                maxStackSize = 3;
                minStackSize = 1;
                sliceMethod = SliceMethod.Random;
                Debug.LogError("Pig detected: Slice: Random - Max Size: 3 - Min Size: 1");
            }
            else if (brick.labelName == "lion")
            {
                maxStackSize = 4;
                minStackSize = 2;
                sliceMethod = SliceMethod.Min;
                
                Debug.LogError("Lion detected: Slice: Min - Max Size: 4 - Min Size: 2");
            }
        });
        
        if (prevPenguin != currPenguin)
        {
            prevPenguin = currPenguin;
        }
        else
        {
            currPenguin = 0;
            prevPenguin = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //ClearText();
        if (runOnce)
        {
            GetRoom();
        }

        if (bricks.Count >= 0)
        {
            //adminMenuVisuals.ForEach(Destroy);
            //adminMenuVisuals.Clear();
            rectPos1.SetActive(debugMode);
            rectPos2.SetActive(debugMode);
            rectCenter.SetActive(debugMode);

            // penguinPosCircle.transform.position = smallPenguinPos;
            if (debugMode)
            {
                //DebugMenu();
                RunDebugMenu();
                DisplayRect();
            }

            if (state == GameState.Setup && !runOnce)
            {
                Setup();
            }
            else if (state == GameState.Separate)
            {
                //Seprate();
            }
            else if (state == GameState.Play)
            {
                Play();
            }
        }
    }

    private void DisplayRect()
    {

    }

    private void Setup()
    {
        ResetBricksInFrame();

        drawnBricks.ForEach(Destroy);
        drawnBricks.Clear();

        bricksInFrame = GetBricksInFrame(bricks, debugMode);
        //GameUtils.AddText(centerCam, canvas, setupText, displayPos + new Vector3(0,0.15f,0), Color.white, 3);
        mainText.transform.position = displayPos + new Vector3(0, 0.15f, 0);
        mainText.transform.GetComponentInChildren<TMP_Text>().text = setupText;
        mainText.transform.GetChild(0).LookAt(centerCam);
        mainText.transform.GetChild(0).Rotate(Vector3.up, 180);

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
        //GameUtils.AddText(centerCam, canvas, playText, displayPos + new Vector3(0, 0.15f, 0), Color.white, 3);
        mainText.transform.position = displayPos + new Vector3(0, 0.15f, 0);
        mainText.transform.GetComponentInChildren<TMP_Text>().text = playText;
        mainText.transform.GetChild(0).LookAt(centerCam);
        mainText.transform.GetChild(0).Rotate(Vector3.up, 180);
        


        //if (debugMode)
        //{
        //    GameUtils.AddText(centerCam, canvas, "Debug mode \"On\". \nTo disable detect \"big penguin\".", displayPos + offsetDir * 0.18f + new Vector3(0, 0.15f, 0), Color.white, 1.5f);
        //}

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

            if (stacksInFrame.Count == 0)
            {
                return;
            }
            
            stacksInFrame = FindStacksInFrame(FixStacks(stacksInFrame, bricks));
            
            if (stacksInFrame.Count == 0)
            {
                return;
            }

            if (debugMode)
            {
                DrawDebugStacks(stacksInFrame);
            }
            
            float[,] distMat = GameUtils.PointsStackDistansMat(stacksInFrame, spawnPoints);
            List<int> ints = GameUtils.ClosestStacks(distMat);

            //if (debugMode)
            //{
            //    DrawStackPositions(stacksInFrame, distMat, ints);
            //}

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (distMat[i, ints[i]] < distThreshold)
                {
                    spawnpointNoDetectionCounts[i] = 0;

                    List<string> placedStack = GameUtils.DetectedObjectListToStringList(stacksInFrame[ints[i]]);

                    if (GameUtils.HaveSameElementAtSameIndex(stacksToBuild[i], placedStack) || complted[i])
                    {
                        if (!complted[i])
                        {
                            var particleSystems = spawnPoints[i].GetComponentsInChildren<ParticleSystem>();
                        
                            foreach (var system in particleSystems)
                            {
                                system.Play();
                            }
                        }
                        var col = Color.green;
                        col.a = 0.33f;
                        spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = col;
                        complted[i] = true;

                    }
                    else if (GameUtils.HaveSameElements(stacksToBuild[i], placedStack))
                    {
                        spawnpointWringStackCounts[i] = 0;
                        var col = Color.yellow;
                        col.a = 0.33f;
                        spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = col;
                    }
                    else
                    {
                        spawnpointWringStackCounts[i]++;
                        if (spawnpointWringStackCounts[i] > 5)
                        {
                            var col = Color.red;
                            col.a = 0.33f;
                            spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = col;
                            spawnpointWringStackCounts[i] = 6;
                        }
                    }
                }
                else
                {
                    spawnpointNoDetectionCounts[i]++;
                    if (complted[i])
                    {
                        var col = Color.green;
                        col.a = 0.33f;
                        spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = col;
                    }
                    else if (spawnpointNoDetectionCounts[i] > 5)
                    {
                        var col = Color.white;
                        col.a = 0.33f;
                        spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = col;
                        spawnpointNoDetectionCounts[i] = 6;
                    }
                }
            }

            taskComplet = CheckIfAllDone();
            if (taskComplet)
            {
                levelsComplteded++;
            }
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
        Vector3 menuPos = rectCenter.transform.position + offsetDir * -0.2f;
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
                planeCenter = Vector3.Lerp(rectPos1.transform.position, rectPos2.transform.position, 0.5f);
                rectCenter.transform.position = planeCenter;
                break;

            case 9:
                ourPlaneRect = (Rect)tableAnchor.PlaneRect;
                rectCenter.transform.position = tableAnchor.transform.position;
                break;

            default:
                break;
        }
        adminAction = -1;

    }

    private void RunDebugMenu()
    {

        UpdateDebugText();

        if (canDoAdminInteraction)
        {
            debugMenu.transform.GetChild(0).GetComponent<Renderer>().material.color = Color.green;
        }
        else
        {
            debugMenu.transform.GetChild(0).GetComponent<Renderer>().material.color = Color.red;
        }

        int adminAction = -1;

        for (int i = 0; i < debugMenu.transform.childCount; i++)
        {
            float d = Vector3.Distance(smallPenguinPos, debugMenu.transform.GetChild(i).position);

            if (Vector3.Distance(smallPenguinPos, debugMenu.transform.GetChild(0).position) < 0.03)
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
                if (maxStackSize + 1 <= briksToBuildStack.Values.Sum())
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
                levelsComplteded++;
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
                ourPlaneRect = new Rect(minX, minY, maxX - minX, maxY - minY);
                UpdateRectDispaly(ourPlaneRect);
                planeCenter = Vector3.Lerp(rectPos1.transform.position, rectPos2.transform.position, 0.5f);
                rectCenter.transform.position = planeCenter;
                break;

            case 9:
                ourPlaneRect = (Rect)tableAnchor.PlaneRect;
                UpdateRectDispaly(ourPlaneRect);
                rectCenter.transform.position = tableAnchor.transform.position;
                break;

            case 10:
                debugHand.SetActive(!debugHand.activeSelf);
                break;

            default:
                break;
        }
        adminAction = -1;
    }

    private void UpdateDebugText()
    {
        string[] texts = new string[11] {
                         "Debug mode \"On\". \nTo disable detect \"big penguin\".",
                         "+1 to max size \nCurrent max size: " + maxStackSize,
                         "-1 from max size \nCurrent max size: " + maxStackSize,
                         "+1 to min size \nCurrent min size: " + minStackSize,
                         "-1 from min size \nCurrent min size: " + minStackSize,
                         "Change slice mode\nCurrent: " + sliceMethod.ToString(),
                         "Complet task",
                         "To setup",
                         "Make new rect",
                         "Reset rect",
                         "Hand display: "+ debugHand.activeSelf };


        for (int i = 0; i < debugTestObjects.Count; i++)
        {
            debugTestObjects[i].transform.GetComponent<TMP_Text>().text = texts[i];
            if (i == 0)
            {
                debugTestObjects[i].transform.position = debugMenu.transform.GetChild(i).position + new Vector3(0, 0.12f, 0);
            }
            else
            {
                debugTestObjects[i].transform.position = debugMenu.transform.GetChild(i).position + new Vector3(0, 0.05f, 0);
            }
            //debugMenu.transform.GetChild(i).transform.GetChild(0).localScale = new Vector3(4f, 4f, 4f);
            //debugMenu.transform.GetChild(i).transform.GetChild(0).rotation = Quaternion.identity;
            debugTestObjects[i].transform.LookAt(centerCam);
            debugTestObjects[i].transform.Rotate(Vector3.up, 180);
            //debugMenu.transform.GetChild(i).transform.GetChild(0).Rotate(debugMenu.transform.right, 90);

        }
    }

    private void MakeDebugMenu(GameObject parent)
    {
        //GameObject debugMenuText = new GameObject("DebugMenuText");
        //debugMenuText.transform.parent = null;

        Vector3 menuPos = rectCenter.transform.position;
        Vector3 xOffset = new Vector3(0.07f, 0, 0);

        GameObject adminpoint = GameUtils.MakeInteractionCirkle(menuPos + parent.transform.forward * 0.1f, Color.red);
        adminpoint.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Debug mode \"On\". \nTo disable detect \"big penguin\".", menuPos + parent.transform.forward * 0.18f + new Vector3(0, 0.15f, 0), Color.white, 1.5f));

        GameObject addMax = GameUtils.MakeInteractionCirkle(menuPos - xOffset * 4, Color.blue);
        addMax.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("+1 to max size \nCurrent max size: " + maxStackSize, menuPos - xOffset * 4 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject removeMax = GameUtils.MakeInteractionCirkle(menuPos - xOffset * 3, Color.blue);
        removeMax.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("-1 from max size \nCurrent max size: " + maxStackSize, menuPos - xOffset * 3 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject addMin = GameUtils.MakeInteractionCirkle(menuPos - xOffset * 2, Color.blue);
        addMin.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("+1 to min size \nCurrent min size: " + minStackSize, menuPos - xOffset * 2 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject removeMin = GameUtils.MakeInteractionCirkle(menuPos - xOffset * 1, Color.blue);
        removeMin.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("-1 from min size \nCurrent min size: " + minStackSize, menuPos - xOffset * 1 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject newSilceMode = GameUtils.MakeInteractionCirkle(menuPos + xOffset * 0, Color.blue);
        newSilceMode.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Change slice mode\nCurrent: " + sliceMethod.ToString(), menuPos + xOffset * 0 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject completTask = GameUtils.MakeInteractionCirkle(menuPos + xOffset * 1, Color.blue);
        completTask.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Complet task", menuPos + xOffset * 1 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject toSetup = GameUtils.MakeInteractionCirkle(menuPos + xOffset * 2, Color.blue);
        toSetup.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("To setup", menuPos + xOffset * 2 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject makeNewRect = GameUtils.MakeInteractionCirkle(menuPos + xOffset * 3, Color.blue);
        makeNewRect.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Make new rect", menuPos + xOffset * 3 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject resteRect = GameUtils.MakeInteractionCirkle(menuPos + xOffset * 4, Color.blue);
        resteRect.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Reset rect", menuPos + xOffset * 4 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject handDispaly = GameUtils.MakeInteractionCirkle(menuPos + xOffset * 5, Color.blue);
        handDispaly.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Hand display: " + debugHand.activeSelf, menuPos + xOffset * 5 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

    }

    private void UpdateRectDispaly(Rect rect)
    {
        float minX = rect.xMin;
        float minY = rect.yMin;
        float maxX = rect.xMax;
        float maxY = rect.yMax;

        rectDisplay.transform.GetChild(0).transform.position = tableAnchor.transform.position - new Vector3(minX, 0, minY);
        rectDisplay.transform.GetChild(1).transform.position = tableAnchor.transform.position - new Vector3(minX, 0, maxY);
        rectDisplay.transform.GetChild(2).transform.position = tableAnchor.transform.position - new Vector3(maxX, 0, minY);
        rectDisplay.transform.GetChild(3).transform.position = tableAnchor.transform.position - new Vector3(maxX, 0, maxY);
    }

    private void MakeRectDisplay(GameObject rectDisplay)
    {
        Rect rect = ourPlaneRect;
        GameObject pos1 = GameUtils.MakeInteractionCirkle(new Vector3(rect.xMin, 0, rect.yMin), Color.magenta);
        GameObject pos2 = GameUtils.MakeInteractionCirkle(new Vector3(rect.xMin, 0, rect.yMax), Color.magenta);
        GameObject pos3 = GameUtils.MakeInteractionCirkle(new Vector3(rect.xMax, 0, rect.yMin), Color.magenta);
        GameObject pos4 = GameUtils.MakeInteractionCirkle(new Vector3(rect.xMax, 0, rect.yMax), Color.magenta);
        pos1.transform.parent = rectDisplay.transform;
        pos2.transform.parent = rectDisplay.transform;
        pos3.transform.parent = rectDisplay.transform;
        pos4.transform.parent = rectDisplay.transform;
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
                GameObject cube = Instantiate(GameManager.Instance.GetBrick(stack[i].labelName), pos + offset + new Vector3(0, 0.03f, 0) * i, Quaternion.identity, cubeParent.transform.GetChild(1));
            }

            offset += new Vector3(0.05f, 0, 0);
        }
    }

    private List<List<DetectedObject>> FindStacksInFrame(List<DetectedObject> detectedBricks)
    {
        List<List<DetectedObject>> stacksColor = new List<List<DetectedObject>>();
        if (detectedBricks == null || detectedBricks.Count == 0)
        {
            return stacksColor;
        }
        float[,] distArr = new float[1, 1];
        if (detectedBricks.Count > 1)
        {
            distArr = GameUtils.DistMat(detectedBricks);
        }

        int[,] ids = GameUtils.ClosestBricks(distArr, stackThreshold);

        List<List<int>> stacks = GameUtils.FindConnectedComponents(ids);
        
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

            //Turtoial stuf
            if (levelsComplteded == 0)
            {
                stacksToBuild = turtoial1;
            }
            else if (levelsComplteded == 1)
            {
                stacksToBuild = turtoial2;
            }

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
                GameUtils.AddText(centerCam, canvas, "No spawnPoints",displayPos, Color.white);
            }


            for (int i = 0; i < stacksToBuild.Count; i++)
            {
                List<GameObject> tempStack = GameUtils.DrawStack(stacksToBuild[i], spawnPoints[i].position + new Vector3(0,0.015f,0) + offsetDir * 0.07f);
                foreach (GameObject item in tempStack)
                {
                    item.transform.parent = cubeParent.transform.GetChild(0);
                }
                GameObject cirkel = GameUtils.MakeInteractionCirkle(spawnPoints[i].position, Color.white);
                cirkel.transform.localScale = new Vector3(0.08f, 0.001f, 0.08f);
                cirkel.transform.parent = spawnPoints[i];
            }

            dists = new float[stacksToBuild.Count];
            complted = new bool[stacksToBuild.Count];
            spawnpointNoDetectionCounts = new int[stacksToBuild.Count];
            spawnpointWringStackCounts = new int[stacksToBuild.Count];
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
            offsetDir = (new Vector3(anchorPoint.x, 0, anchorPoint.z) - new Vector3(centerCam.position.x, 0, centerCam.position.z)).normalized;

            displayPos = anchorPoint + offsetDir * 0.25f;

            ourPlaneRect = (Rect)tableAnchor.PlaneRect;
            UpdateRectDispaly(ourPlaneRect);

            mainText.transform.position = displayPos + new Vector3(0,0.15f,0);

            rectPos1.transform.parent = tableAnchor.transform;

            rectPos2.transform.parent = tableAnchor.transform;

            rectCenter.transform.position = tableAnchor.transform.position;

            debugMenu.transform.position = tableAnchor.transform.position + offsetDir * -0.2f;

            debugHand.transform.position = tableAnchor.transform.position + offsetDir * 0.4f;
            debugHand.transform.Rotate(offsetDir, -90);
            debugHand.SetActive(false);
        }
    }
}


public enum GameState
{
    Setup,
    Play,
    Separate
}

