using System.Collections;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Meta.WitAi.Composer;

public class PlaceStackGame : MonoBehaviour
{
    public ObjectDetector objectDetection;
    public GameObject spawnPositions;
    public Transform centerCam;
    public GameObject canvas;
    public GameObject cubeParent;

    public GameObject progressBuildPrefab;
    ProgressBuild progressBuild;
    
    public GameObject debugHand;
    public Transform[] spawnPoints;
    
    public List<List<GameObject>> visualStacks = new List<List<GameObject>>();

    public int maxStackSize = 4;
    public int minStackSize = 1;
    public SliceMethod sliceMethod = SliceMethod.Min;
    public int maxNumberOfBriksToUse = 8;
    public float maxDistHeadToSpwanpoint = 1.0f;
    public float minDistHeadToSpwanpoint = 0.10f;
    public float distToPointThreshold = 0.08f;
    public float stackThreshold = 0.06f;

    public bool musicOn = true;

    bool taskComplet = false;
    bool makeNewLevel = false;
    bool endlessMode = false;

    Dictionary<string, int> bricksInFrame = new Dictionary<string, int>();
    Dictionary<string, int> briksToBuildStack = new Dictionary<string, int> { { "red", 1 }, { "green", 2 }, { "blue", 2 }, { "yellow", 3 }, { "magenta", 0 } };
    public List<List<string>> stacksToBuild = new();

    private GameState state = GameState.Setup;
    string[] objectsToDetect = { "red", "green", "blue", "yellow" };

    List<GameObject> drawnBricks = new List<GameObject>();

    float[] dists;
    public bool[] complted;
    int[] spawnpointNoDetectionCounts;
    int[] spawnpointWrongStackCounts;
    int[] spawnpointRightStackCounts;

    List<DetectedObject> bricks = new List<DetectedObject>();
    List<List<DetectedObject>> stacksInFrame = new List<List<DetectedObject>>();

    MRUKRoom room;
    List<MRUKAnchor> anchors = new();
    public MRUKAnchor tableAnchor = null;
    public Rect ourPlaneRect = new Rect(1, 1, -1, -1);
    GameObject rectPos1;
    GameObject rectPos2;
    GameObject rectCenter;
    Vector3 planeCenter;

    Vector3 anchorPoint = new Vector3(0, 0, 0);
    Vector3 displayAnchor = new Vector3(0, 0, 0);
    Vector3 offsetDir = new Vector3(0, 0, 0);
    Vector3 displayPos = new Vector3();

    PointSide pointSide = PointSide.Both;

    GameObject debugMenu;
    List<GameObject> debugTestObjects = new List<GameObject>();
    GameObject rectDisplay;
    Vector3 smallPenguinPos = new Vector3(10, 10, 10);
    string debugCurrentSettingsText = "";

    GameObject mainText;

    bool runOnce = true;
    bool debugMode = false;
    bool canDoAdminInteraction = false;
    bool showAllPoints = false;
    private bool levelReset;

    int levelsComplteded = 0;

    string setupText = "Start med at finde de klodser der skal bruges.\nRød: 1, Blå: 2, Grøn: 2, Gul: 3"; //"To start find the needed bricks.";
    string playText = "Stabel klodserne som vist og placer dem på den tilhørene cirkel."; //"Build the displayed stacks and place them in the circle.";

    private int prevPenguin = 0;
    private int currPenguin = 0;

    List<List<string>> turtoial1 = new List<List<string>>
    {
        new List<string>{ "red"},
        new List<string>{ "blue" },
    };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        objectDetection.OnObjectsDetected += HandleBricksDetected;
        objectDetection.OnStacksDetected += HandleStacksDetected;

        if(progressBuildPrefab == null)
        {
            Debug.LogError("ProgressBuild prefab is not assigned in the inspector.");
            return;
        }
        else
        {
            progressBuild = progressBuildPrefab.GetComponent<ProgressBuild>();
        }

        rectPos1 = GameUtils.MakeInteractionCirkle(new Vector3(0, 0, 0), Color.cyan);
        rectPos1.transform.localScale = new Vector3(0.03f, 0.001f, 0.03f);

        rectPos2 = GameUtils.MakeInteractionCirkle(new Vector3(0, 0, 0), Color.cyan);
        rectPos2.transform.localScale = new Vector3(0.03f, 0.001f, 0.03f);

        rectCenter = GameUtils.MakeInteractionCirkle(new Vector3(0, 0, 0), Color.green);
        rectCenter.transform.localScale = new Vector3(0.03f, 0.001f, 0.03f);

        mainText = new GameObject("MainText");
        GameUtils.AddText(centerCam, mainText, setupText, new Vector3(0, 0, 0), Color.white, 3f);

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

        if (musicOn)
        {
            AudioManager.Instance.ChangeMusic(AudioManager.SoundType.Background_Music);
        }
  
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
                maxStackSize = 5;
                minStackSize = 2;
                maxNumberOfBriksToUse = 8;
                sliceMethod = SliceMethod.Random;
                minDistHeadToSpwanpoint = 0.2f;
                maxDistHeadToSpwanpoint = 0.6f;
                pointSide = PointSide.Both;
                Debug.LogError("Human detected: Slice: Random - Max Size: 5 - Min Size: 2 - Nr. briks: 8 - Range: 0.2-0.6");

            }
            else if (brick.labelName == "sheep" && !debugMode)
            {
                //One big stack
                maxStackSize = 8;
                minStackSize = 4;
                maxNumberOfBriksToUse = 8;
                sliceMethod = SliceMethod.Max;
                minDistHeadToSpwanpoint = 0.1f;
                maxDistHeadToSpwanpoint = 0.4f;
                pointSide = PointSide.Both;
                Debug.LogError("Sheep detected: Slice: Max - Max Size: 8 - Min Size: 4 - Nr. briks: 8 - Range: 0.1-0.4");

                if (progressBuild.IsBuildComplete())
                {
                    foreach (var t in spawnPoints)
                    {
                        t.gameObject.SetActive(true);
                    }
                    foreach (Transform t in cubeParent.transform.GetChild(0))
                    {
                        t.gameObject.SetActive(true);
                    }
                    endlessMode = true;
                    progressBuild.ResetProgress();
                    state = GameState.Play;
                }
            }
            else if (brick.labelName == "pig" && !debugMode)
            {
                //many small stacks
                maxStackSize = 3;
                minStackSize = 1;
                maxNumberOfBriksToUse = 6;
                sliceMethod = SliceMethod.Random;
                minDistHeadToSpwanpoint = 0.5f;
                maxDistHeadToSpwanpoint = 0.7f;
                pointSide = PointSide.Both;
                Debug.LogError("Pig detected: Slice: Random - Max Size: 3 - Min Size: 1 - Nr. briks: 6 - Range: 0.5-0.7");
            }
            else if (brick.labelName == "lion")
            {
                int newMin = Random.Range(1, 5);
                int newMax = Random.Range(5, 9);
                float newMinDist = Random.Range(0.1f, 0.3f);
                float newMaxDist = Random.Range(0.5f, 0.8f);

                minStackSize = newMin;
                maxStackSize = newMax;
                minDistHeadToSpwanpoint = newMinDist;
                maxDistHeadToSpwanpoint = newMaxDist;

                sliceMethod = (SliceMethod)Random.Range(0, 4);

                maxNumberOfBriksToUse = Random.Range(6, 9);
                Debug.LogError($"Lion detected: Slice: {sliceMethod} - Max Size: {maxStackSize} - Min Size: {minStackSize} - Nr. briks: {maxNumberOfBriksToUse} - Range: {minDistHeadToSpwanpoint.ToString("0.00")}-{maxDistHeadToSpwanpoint.ToString("0.00")}");
            }
        });

        debugCurrentSettingsText = $"Current Settings:\n" +
                                   $"Max number of bricks to use: {maxNumberOfBriksToUse}\n" +
                                   $"Min stack size: {minStackSize}\n" +
                                   $"Max stack size: {maxStackSize}\n" +
                                   $"Slice method: {sliceMethod}\n" +
                                   $"Min dist head to spawnpoint: {minDistHeadToSpwanpoint}\n" +
                                   $"Max dist head to spawnpoint: {maxDistHeadToSpwanpoint}";

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

    private void HandleStacksDetected(object sender, StackDetectedEventArgs e)
    {
        stacksInFrame = new List<List<DetectedObject>>();
        e.DetectedStacks.ForEach(stack =>
        {
            List<DetectedObject> tempStack = new List<DetectedObject>();
            foreach (var brick in bricks)
            {
                if (stack.Contains(brick))
                {
                    tempStack.Add(brick);
                }
            };

            if (tempStack.Count > 0)
            {
                tempStack.Sort((a, b) => a.screenPos.y.CompareTo(b.screenPos.y));
                stacksInFrame.Add(tempStack);
            }
        });

        if (state == GameState.Play)
        {
            CalculateStacks();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (runOnce)
        {
            GetRoom();
        }

        if (bricks.Count >= 0)
        {
            //Debug mode
            rectPos1.SetActive(debugMode);
            rectPos2.SetActive(debugMode);
            rectCenter.SetActive(debugMode);

            if (debugMode)
            {
                RunDebugMenu();
            }

            if (state == GameState.Setup && !runOnce)
            {
                Setup();
            }
            else if (state == GameState.Play)
            {
                Play();
            }
            else if (state == GameState.End)
            {
                End();
            }
        }
    }

    private void Setup()
    {
        ResetBricksInFrame();

        drawnBricks.ForEach(Destroy);
        drawnBricks.Clear();

        bricksInFrame = GetBricksInFrame(bricks, debugMode);
        mainText.transform.position = displayPos + new Vector3(0, 0.15f, 0);
        mainText.transform.GetComponentInChildren<TMP_Text>().text = setupText;
        mainText.transform.GetChild(0).LookAt(centerCam);
        mainText.transform.GetChild(0).Rotate(Vector3.up, 180);

        //Check if we have all the bricks needed to play the game
        if ((bricksInFrame["red"] == 1 && bricksInFrame["green"] == 2 && bricksInFrame["blue"] == 2 && bricksInFrame["yellow"] == 3))
        {
            drawnBricks.ForEach(Destroy);
            drawnBricks.Clear();
            makeNewLevel = true;
            state = GameState.Play;
        }
    }

    private void Play()
    {
        if (levelReset) return;
        mainText.transform.position = displayPos + new Vector3(0, 0.25f, 0);
        mainText.transform.GetChild(0).LookAt(centerCam);
        mainText.transform.GetChild(0).Rotate(Vector3.up, 180);

        if (makeNewLevel)
        {
            DestroyCubes(0);
            NewTable();
        }
        else
        {
            taskComplet = CheckIfAllDone();
            if (taskComplet && !levelReset)
            {
                levelsComplteded++;
                if(progressBuild != null && !endlessMode)
                {
                    //progressBuild.IncrementProgress();
                    progressBuild.IncrementRandom();
                }
                DataLogger.Log($"stack","S_EVENT:FINISHED");
                
                if (progressBuild.IsBuildComplete() && !endlessMode)
                {
                    state = GameState.End;
                    return;
                }

                StartCoroutine(WaitForTaskComplete());
            }
        }
    }

    public void End()
    {
        foreach (var t in spawnPoints)
        {
            t.gameObject.SetActive(false);
        }
        foreach (Transform t in cubeParent.transform.GetChild(0))
        {
            t.gameObject.SetActive(false);
        }

        mainText.transform.position = displayPos + new Vector3(0, 0.30f, 0);
        mainText.transform.GetChild(0).LookAt(centerCam);
        mainText.transform.GetChild(0).Rotate(Vector3.up, 180);
        mainText.GetComponentInChildren<TMP_Text>().text = "Opgave løst.\nGodt gjort!";

        progressBuild.transform.position = displayAnchor + offsetDir * 0.4f;
        progressBuild.transform.Rotate(Vector3.up, 10 * Time.deltaTime);
    }

    private void CalculateStacks()
    {
        if (levelReset)
        {
            mainText.GetComponentInChildren<TMP_Text>().text = "Opgave løst.\nGodt gjort!";
        }
        else if (endlessMode)
        {
            mainText.transform.GetComponentInChildren<TMP_Text>().text = playText + $"\nNiveauer gennemført: {levelsComplteded}";
        }
        else
        {
            mainText.transform.GetComponentInChildren<TMP_Text>().text = playText;
        }

        float[,] distMat = GameUtils.PointsStackDistansMat(stacksInFrame, spawnPoints);
        List<int> ints = GameUtils.ClosestStacks(distMat);

        DestroyCubes(1);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (stacksInFrame.Count == 0){ continue;}
            if (!spawnPoints[i]) continue;

            if (distMat[i, ints[i]] < distToPointThreshold)
            {
                spawnpointNoDetectionCounts[i] = 0;
                List<string> placedStack = GameUtils.DetectedObjectListToStringList(stacksInFrame[ints[i]]);

                if (GameUtils.HaveSameElementAtSameIndex(stacksToBuild[i], placedStack)|| complted[i])
                {
                    spawnpointRightStackCounts[i]++;
                    if (spawnpointRightStackCounts[i] > 1 || complted[i])
                    {
                        if (!complted[i])
                        {
                            complted[i] = true;

                            var particleSystems = spawnPoints[i].GetComponentsInChildren<ParticleSystem>();

                            foreach (var system in particleSystems)
                            {
                                system.Play();
                            }
                            
                            DataLogger.Log($"stack",$"S_EVENT:COMPLETED;NUM_STACKNUM:{i}");
                            
                            if(CheckIfAllDone())
                            {
                                AudioManager.Instance.Play(AudioManager.SoundType.Level_Complete);
                            }
                            else
                            {
                                AudioManager.Instance.Play(AudioManager.SoundType.Stack_Complete);
                            }
                           
                        }
                        var col = Color.green;
                        col.a = 0.33f;
                        spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = col;
                        spawnpointRightStackCounts[i] = 2;
                    }
                    spawnpointWrongStackCounts[i] = 0;
                }
                else
                {
                    spawnpointWrongStackCounts[i]++;
                    if (spawnpointWrongStackCounts[i] > 2)
                    {
                        if (GameUtils.HaveSameElements(stacksToBuild[i],placedStack))
                        {
                            var col = Color.yellow;
                            col.a = 0.33f;
                            spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = col;
                        }
                        else
                        {
                            var col = Color.red;
                            col.a = 0.33f;
                            spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = col;
                        }

                        if (spawnpointWrongStackCounts[i] > 8)
                        {
                            spawnpointRightStackCounts[i]--;
                            spawnpointWrongStackCounts[i] = 3;
                        }
                        if (spawnpointRightStackCounts[i] < 0) { spawnpointRightStackCounts[i] = 0; }
                        
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
                else if (spawnpointNoDetectionCounts[i] > 3)
                {
                    var col = Color.white;
                    col.a = 0.33f;
                    spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = col;
                    spawnpointNoDetectionCounts[i] = 4;
                    spawnpointWrongStackCounts[i] = 0;
                    spawnpointRightStackCounts[i] = 0;
                }
            }
        }
        
        if (debugMode)
        {
            //Display the stacks the model ses
            DrawDebugStacks(stacksInFrame);
            //For each stack in the frame, draw a circle at the world position of the stack. This is used for claculating the distance to the points
            for (int i = 0; i < stacksInFrame.Count; i++)
            {
                GameObject temp = GameUtils.MakeInteractionCirkle(stacksInFrame[i][0].worldPos, Color.gray);
                temp.transform.localScale = new Vector3(0.02f, 0.001f, 0.02f);
                temp.transform.parent = cubeParent.transform.GetChild(1);
            }
        }
    }

    IEnumerator WaitForTaskComplete()
    {
        levelReset = true;
        foreach (var t in spawnPoints)
        {
            t.gameObject.SetActive(false);
        }
        BrickDisappear.DisappearStacks(visualStacks);
        yield return new WaitForSeconds(4);
        visualStacks.Clear();
        makeNewLevel = true;
        levelReset = false;
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

            if (Vector3.Distance(smallPenguinPos, debugMenu.transform.GetChild(0).position) < 0.05)
            {
                canDoAdminInteraction = true;
            }

            if (d < 0.04 && canDoAdminInteraction)
            {
                adminAction = i;
                canDoAdminInteraction = false;
            }
        }

        switch (adminAction)
        {
            case 1:
                musicOn = !musicOn;

                if (musicOn)
                {
                    AudioManager.Instance.ChangeMusic(AudioManager.SoundType.Background_Music);
                }
                else
                {
                    AudioManager.Instance.StopMusic();
                }
                break;

            case 2:
                CalibratePosition();
                break;

            case 3:
                showAllPoints = !showAllPoints;
                break;

            case 4:
                for (int i = 0; i < complted.Length; i++)
                {
                    complted[i] = true;
                }
                break;

            case 5:
                float minX = Mathf.Min(rectPos1.transform.localPosition.x, rectPos2.transform.localPosition.x);
                float maxX = Mathf.Max(rectPos1.transform.localPosition.x, rectPos2.transform.localPosition.x);
                float minY = Mathf.Min(rectPos1.transform.localPosition.y, rectPos2.transform.localPosition.y);
                float maxY = Mathf.Max(rectPos1.transform.localPosition.y, rectPos2.transform.localPosition.y);
                ourPlaneRect = new Rect(minX, minY, maxX - minX, maxY - minY);
                UpdateRectDispaly(ourPlaneRect);
                planeCenter = Vector3.Lerp(rectPos1.transform.position, rectPos2.transform.position, 0.5f);
                rectCenter.transform.position = planeCenter;
                break;

            case 6:
                ourPlaneRect = (Rect)tableAnchor.PlaneRect;
                UpdateRectDispaly(ourPlaneRect);
                rectCenter.transform.position = tableAnchor.transform.position;
                break;

            case 7:
                debugHand.SetActive(!debugHand.activeSelf);
                break;

            default:
                break;
        }
        adminAction = -1;
    }

    private void UpdateDebugText()
    {
        string[] texts = new string[9] {
                         "Debug mode \"On\". \nTo disable detect \"big penguin\".",
                         debugCurrentSettingsText,
                         "Music On: " + musicOn,
                         "Recalibrate",             
                         "Show all points:" + showAllPoints,
                         "Complet task",
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
            else if (i == 1)
            {
                debugTestObjects[i].transform.position = debugMenu.transform.GetChild(0).position + new Vector3(0, 0.18f, 0) + debugMenu.transform.right * 0.18f;
            }
            else
            {
                debugTestObjects[i].transform.position = debugMenu.transform.GetChild(i-1).position + new Vector3(0, 0.05f, 0);
            }
            debugTestObjects[i].transform.LookAt(centerCam);
            debugTestObjects[i].transform.Rotate(Vector3.up, 180);
        }
    }

    private void MakeDebugMenu(GameObject parent)
    {
        Vector3 menuPos = rectCenter.transform.position;
        Vector3 xOffset = new Vector3(0.07f, 0, 0);

        GameObject adminpoint = GameUtils.MakeInteractionCirkle(menuPos + parent.transform.forward * 0.1f, Color.red);
        adminpoint.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Debug mode \"On\". \nTo disable detect \"big penguin\".", menuPos + parent.transform.forward * 0.18f + new Vector3(0, 0.15f, 0), Color.white, 1.5f));

        debugTestObjects.Add(GameUtils.AddText("Current Settings", menuPos + parent.transform.forward * 0.18f + new Vector3(0, 0.018f, 0) + transform.right * 0.18f, Color.white, 1f));

        GameObject music = GameUtils.MakeInteractionCirkle(menuPos + xOffset * -4.5f, Color.blue);
        music.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Music On: " + musicOn, menuPos + xOffset * -4.5f + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject recalibrater = GameUtils.MakeInteractionCirkle(menuPos + xOffset * -3f, Color.blue);
        recalibrater.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Recalibrate" + showAllPoints, menuPos + xOffset * -3f + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject pointShower = GameUtils.MakeInteractionCirkle(menuPos + xOffset * -1.5f, Color.blue);
        pointShower.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Show all points: " + showAllPoints, menuPos + xOffset * -1.5f + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject completTask = GameUtils.MakeInteractionCirkle(menuPos + xOffset * 0, Color.blue);
        completTask.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Complet task", menuPos + xOffset * 0 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject makeNewRect = GameUtils.MakeInteractionCirkle(menuPos + xOffset * 1.5f, Color.blue);
        makeNewRect.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Make new rect", menuPos + xOffset * 1.5f + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject resteRect = GameUtils.MakeInteractionCirkle(menuPos + xOffset * 3f, Color.blue);
        resteRect.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Reset rect", menuPos + xOffset * 3f + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject handDispaly = GameUtils.MakeInteractionCirkle(menuPos + xOffset * 4.5f, Color.blue);
        handDispaly.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Hand display: " + debugHand.activeSelf, menuPos + xOffset * 4.5f + new Vector3(0, 0.01f, 0), Color.white, 0.8f));
    }

    private void UpdateRectDispaly(Rect rect)
    {
        rectDisplay.transform.GetChild(0).transform.position = tableAnchor.transform.position + tableAnchor.transform.right * rect.width / 2 + tableAnchor.transform.up * rect.height / 2;
        rectDisplay.transform.GetChild(1).transform.position = tableAnchor.transform.position + tableAnchor.transform.right * rect.width / 2 - tableAnchor.transform.up * rect.height / 2;
        rectDisplay.transform.GetChild(2).transform.position = tableAnchor.transform.position - tableAnchor.transform.right * rect.width / 2 - tableAnchor.transform.up * rect.height / 2;
        rectDisplay.transform.GetChild(3).transform.position = tableAnchor.transform.position - tableAnchor.transform.right * rect.width / 2 + tableAnchor.transform.up * rect.height / 2;
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

    private void DrawDebugStacks(List<List<DetectedObject>> stacksInFrame, float yOfset = 0)
    {
        Vector3 pos = displayPos + offsetDir * 0.2f + Vector3.up * yOfset;
        Vector3 offset = new Vector3(0, 0, 0);

        foreach (var stack in stacksInFrame)
        {
            for (int i = 0; i < stack.Count; i++)
            {
                GameObject cube = Instantiate(GameManager.Instance.GetBrick(stack[i].labelName), pos + offset + new Vector3(0, 0.024f, 0) * i, Quaternion.identity, cubeParent.transform.GetChild(1));
            }

            offset += new Vector3(0.07f, 0, 0);
        }
    }

    public void NewTable()
    {
        DestroySpawnPositions();
        if (spawnPositions.transform.childCount == 0)
        {
            //LevelProgress();
    
            List<string> briks = new List<string>();

            foreach (var v in briksToBuildStack)
            {
                for (int i = 0; i < v.Value; i++)
                {
                    briks.Add(v.Key);
                }
            }

            List<string> briksToUse = new List<string>();
            for (int i = 0; i < maxNumberOfBriksToUse; i++)
            {
                int idx = Random.Range(0, briks.Count);
                briksToUse.Add(briks[idx]);
                briks.RemoveAt(idx);
            }

            //Generate stacks to build based on current settings
            stacksToBuild = StackGenerator.GenerateStacks(briksToUse, minStackSize, maxStackSize, sliceMethod);

            TurtoialLevels();

            //if (levelsComplteded == 0)
            //{
            //    stacksToBuild = turtoial1;
            //}

            //Finds the position to place the stacks on the table
            spawnPoints = GameUtils.DiskSampledSpawnPoints(tableAnchor, stacksToBuild.Count, spawnPositions.transform, ourPlaneRect, centerCam, minDistHeadToSpwanpoint, maxDistHeadToSpwanpoint, pointSide, showAllPoints);

            for (int i = 0; i < stacksToBuild.Count; i++)
            {
                List<GameObject> tempStack = GameUtils.DrawStack(stacksToBuild[i], spawnPoints[i].position + new Vector3(0,0.025f,0) + offsetDir * 0.07f);
                visualStacks.Add(tempStack);
                GameObject cirkel = GameUtils.MakeInteractionCirkle(spawnPoints[i].position + new Vector3(0,0.015f,0), Color.white);
                cirkel.transform.localScale = new Vector3(0.08f, 0.001f, 0.08f);
                cirkel.transform.parent = spawnPoints[i];
                
                DataLogger.Log($"stack",$"S_EVENT:GENERATE;NUM_STACKNUM:{i};POS_COORDS:{DataCollection.GetPlaneNormalizedCoordinates(spawnPoints[i].position).ToString("F5")};POS_POSITION:{spawnPoints[i].position.ToString("F5")};LIST_BRICKS:{string.Join(",",stacksToBuild[i])}");

                //A text object foreach point. Manly used for debugging
                GameObject text = GameUtils.AddText("", spawnPoints[i].position + new Vector3(0, 0.06f, 0), Color.white);
                text.transform.parent = cirkel.transform;
            }

            dists = new float[stacksToBuild.Count];
            complted = new bool[stacksToBuild.Count];
            spawnpointNoDetectionCounts = new int[stacksToBuild.Count];
            spawnpointWrongStackCounts = new int[stacksToBuild.Count];
            spawnpointRightStackCounts = new int[stacksToBuild.Count];
            for (int i = 0; i < dists.Length; i++)
            {
                dists[i] = 100;
                complted[i] = false;
            }
            taskComplet = false;
            makeNewLevel = false;
        }
        
        
    }

    private void LevelProgress()
    {
        switch (levelsComplteded)
        {
            case 0:
            case 1:
            case 2:
                maxNumberOfBriksToUse = 2;
                minStackSize = 1;
                maxStackSize = 2;
                sliceMethod = SliceMethod.Min;
                minDistHeadToSpwanpoint = 0.2f;
                maxDistHeadToSpwanpoint = 0.6f;
                break;

            case 3:
            case 4:
            case 5:
                maxNumberOfBriksToUse = 4;
                minStackSize = 2;
                maxStackSize = 3;
                sliceMethod = SliceMethod.Random;
                minDistHeadToSpwanpoint = 0.3f;
                maxDistHeadToSpwanpoint = 0.7f;
                break;

            case 6:
            case 7:
                maxNumberOfBriksToUse = 6;
                minStackSize = 2;
                maxStackSize = 3;
                sliceMethod = SliceMethod.Equalize;
                minDistHeadToSpwanpoint = 0.3f;
                maxDistHeadToSpwanpoint = 0.5f;
                break;

            case 8:
            case 9:
                maxNumberOfBriksToUse = 8;
                maxStackSize = 4;
                minStackSize = 2;
                sliceMethod = SliceMethod.Max;
                minDistHeadToSpwanpoint = 0.2f;
                maxDistHeadToSpwanpoint = 0.55f;
                break;

            case 10:
            case 11:
                maxNumberOfBriksToUse = 8;
                maxStackSize = 8;
                minStackSize = 4;
                sliceMethod = SliceMethod.Max;
                minDistHeadToSpwanpoint = 0.0f;
                maxDistHeadToSpwanpoint = 0.4f;
                break;

            case 12:
            case 13:
                maxNumberOfBriksToUse = 8;
                maxStackSize = 4;
                minStackSize = 2;
                sliceMethod = SliceMethod.Random;
                minDistHeadToSpwanpoint = 0.2f;
                maxDistHeadToSpwanpoint = 0.6f;
                break;

            default:
                if (levelsComplteded >= 15 && levelsComplteded % 5 == 0)
                {
                    int newMin = Random.Range(1, 5);
                    int newMax = Random.Range(5, 9);
                    float newMinDist = Random.Range(0.1f, 0.3f);
                    float newMaxDist = Random.Range(0.5f, 0.8f);
                    
                    minStackSize = newMin;
                    maxStackSize = newMax;
                    minDistHeadToSpwanpoint = newMinDist;
                    maxDistHeadToSpwanpoint = newMaxDist;
                    
                    sliceMethod = (SliceMethod)Random.Range(0, 4);

                    maxNumberOfBriksToUse = Random.Range(6, 9);
                }
                break;
        }
    }

    private void TurtoialLevels()
    {
        switch (levelsComplteded)
        {
            case 0:
                stacksToBuild = new List<List<string>>
                                {
                                    new List<string>{ "red"},
                                    new List<string>{ "blue" },
                                };
                minDistHeadToSpwanpoint = 0.2f;
                maxDistHeadToSpwanpoint = 0.4f;
                break;

            case 1:
                stacksToBuild = new List<List<string>>
                                {
                                    new List<string>{ "red"},
                                    new List<string>{ "green"},
                                    new List<string>{ "blue"},
                                    new List<string>{ "yellow"},
                                };
                minDistHeadToSpwanpoint = 0.3f;
                maxDistHeadToSpwanpoint = 0.7f;
                break;

            case 2:
                stacksToBuild = new List<List<string>>
                                {
                                    new List<string>{ "red", "blue"},
                                    new List<string>{ "green", "yellow" },
                                };
                minDistHeadToSpwanpoint = 0.2f;
                maxDistHeadToSpwanpoint = 0.4f;
                break;

            case 3:
                stacksToBuild = new List<List<string>>
                                {
                                    new List<string>{ "blue", "yellow", "red", "green" },
                                };
                minDistHeadToSpwanpoint = 0.2f;
                maxDistHeadToSpwanpoint = 0.6f;
                break;

            case 4:
                stacksToBuild = new List<List<string>>
                                {
                                    new List<string>{ "yellow"},
                                    new List<string>{ "blue" , "green"},
                                    new List<string>{ "green" , "yellow", "blue", "red"},
                                };
                minDistHeadToSpwanpoint = 0.2f;
                maxDistHeadToSpwanpoint = 0.7f;
                break;

            default:
                break;
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
            //GameObject cObject = Instantiate(GameManager.Instance.cubePrefab, tableAnchor.transform.position, Quaternion.identity);
            //cObject.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            //cObject.GetComponent<Renderer>().material.color = Color.white;

            //GameObject fObject = Instantiate(GameManager.Instance.cubePrefab, tableAnchor.transform.position + tableAnchor.transform.forward * 0.1f, Quaternion.identity);
            //fObject.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            //fObject.GetComponent<Renderer>().material.color = Color.blue;

            //GameObject rObject = Instantiate(GameManager.Instance.cubePrefab, tableAnchor.transform.position + tableAnchor.transform.right * 0.1f, Quaternion.identity);
            //rObject.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            //rObject.GetComponent<Renderer>().material.color = Color.red;

            //GameObject lObject = Instantiate(GameManager.Instance.cubePrefab, tableAnchor.transform.position - tableAnchor.transform.up * 0.1f, Quaternion.identity);
            //lObject.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            //lObject.GetComponent<Renderer>().material.color = Color.green;

            ourPlaneRect = (Rect)tableAnchor.PlaneRect;
            UpdateRectDispaly(ourPlaneRect);

            CalibratePosition();
        }
    }

    private void CalibratePosition()
    {
        displayAnchor = GameUtils.ClosestPointOnQuadEdge(rectDisplay.transform.GetChild(0).position, rectDisplay.transform.GetChild(1).position, rectDisplay.transform.GetChild(2).position, rectDisplay.transform.GetChild(3).position, centerCam.position);
        displayAnchor += new Vector3(0, 0.02f, 0);
        //GameUtils.MakeInteractionCirkle(displayAnchor, Color.magenta);

        offsetDir = (new Vector3(displayAnchor.x, 0, displayAnchor.z) - new Vector3(centerCam.position.x, 0, centerCam.position.z)).normalized;
        
        Vector3 offsetDirX = Vector3.Cross(Vector3.up, offsetDir).normalized;

        // Tjecks if the displayAnchor is in the rect
        if (Vector3.Dot(centerCam.forward, displayAnchor + offsetDir * 0.65f) < 0)
        {
            offsetDir = -offsetDir;
        }

        progressBuild.transform.position = displayAnchor + offsetDir * 0.73f;// + offsetDirX * 0.5f;
        progressBuild.transform.Rotate(Vector3.up, -90);

        displayPos = displayAnchor + offsetDir * 0.65f;

        mainText.transform.position = displayPos + new Vector3(0, 0.25f, 0);

        rectPos1.transform.parent = tableAnchor.transform;
        rectPos2.transform.parent = tableAnchor.transform;
        rectCenter.transform.position = tableAnchor.transform.position;

        debugMenu.transform.position = displayAnchor + offsetDir * 0.15f;

        debugHand.transform.position = displayAnchor + offsetDir * 0.8f;
        debugHand.transform.Rotate(offsetDir, -90);
        debugHand.SetActive(false);
    }
}


public enum GameState
{
    Setup,
    Play,
    End
}

public enum PointSide
{
    Right,
    Left,
    Both
}

