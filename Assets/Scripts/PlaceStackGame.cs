using System.Collections;
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
    public int minStackSize = 1;
    public SliceMethod sliceMethod = SliceMethod.Min;
    public int maxNumberOfBriksToUse = 8;
    public float maxDistHeadToSpwanpoint = 1.0f;
    public float minDistHeadToSpwanpoint = 0.10f;
    public float distToPointThreshold = 0.08f;
    public float stackThreshold = 0.06f;
    public float[] variableThreshold;

    bool taskComplet = false;
    bool makeNewLevel = false;
    bool frezz = false;

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
    int[] spawnpointWrongStackCounts;
    int[] spawnpointWrongOrderCounts;
    int[] spawnpointRightStackCounts;
    bool newDectetion = true;

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

    GameObject pointDevider;
    PointSide pointSide = PointSide.Both;

    GameObject debugMenu;
    List<GameObject> debugTestObjects = new List<GameObject>();
    GameObject rectDisplay;
    Vector3 smallPenguinPos = new Vector3(10, 10, 10);

    GameObject mainText;

    bool runOnce = true;
    bool debugMode = false;
    bool canDoAdminInteraction = true;
    bool showAllPoints = false;
    private bool levelReset;

    int levelsComplteded = 0;

    string setupText = "Start med at de klodser der skal bruges.\nRød: 1, Blå: 2, Grøn: 2, Gul: 3"; //"To start find the needed bricks.";
    string seprateText = "Separate";
    string playText = "Stabel klodserne som vist og placer dem på den tilhørene cirkel."; //"Build the displayed stacks and place them in the circle.";

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
    //GameObject radiusMax;
    //GameObject radiusMin;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        numberOfBricksInGame = briksToBuildStack.Values.Sum();
        _findSpawnPositions = spawnPositions.GetComponent<FindSpawnPositions>();
        _findSpawnPositions.SpawnAmount = numberOfBricksInGame;
        objectDetection.OnObjectsDetected += HandleBricksDetected;
        objectDetection.OnStacksDetected += HandleStacksDetected;

        variableThreshold = new float[8] { 0.08f, 0.08f, 0.08f, 0.08f, 0.10f, 0.012f, 0.14f, 0.16f };

        rectPos1 = GameUtils.MakeInteractionCirkle(new Vector3(0, 0, 0), Color.cyan);
        rectPos1.transform.localScale = new Vector3(0.03f, 0.001f, 0.03f);

        rectPos2 = GameUtils.MakeInteractionCirkle(new Vector3(0, 0, 0), Color.cyan);
        rectPos2.transform.localScale = new Vector3(0.03f, 0.001f, 0.03f);

        rectCenter = GameUtils.MakeInteractionCirkle(new Vector3(0, 0, 0), Color.green);
        rectCenter.transform.localScale = new Vector3(0.03f, 0.001f, 0.03f);

        mainText = new GameObject("MainText");
        GameUtils.AddText(centerCam, mainText, setupText, new Vector3(0, 0, 0), Color.white, 3f);

        //pointDevider = Instantiate(GameManager.Instance.cylinderPrefab, Vector3.zero, Quaternion.identity);
        //pointDevider.transform.localScale = new Vector3(0.01f, 0.01f, 1f);
        // penguinPosCircle = GameUtils.MakeInteractionCirkle(new Vector3(10,10,10),Color.black);
        //radiusMax = GameUtils.MakeInteractionCirkle(new Vector3(0, 0, 0), Color.black);
        //radiusMin = GameUtils.MakeInteractionCirkle(new Vector3(0, 0, 0), Color.white);

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
        //if (!frezz)
        //{
        //    drawnBricks.ForEach(Destroy);
        //    drawnBricks.Clear();
        //}
        bricks = new List<DetectedObject>();
        fixStack = true;
        e.DetectedObjects.ForEach(brick =>
        {
            if (objectsToDetect.Contains(brick.labelName))
            {
                bricks.Add(brick);
                //if (!frezz)
                //{
                //    GameObject cube = Instantiate(GameManager.Instance.GetBrick(brick.labelName), brick.worldPos, Quaternion.identity, cubeParent.transform.GetChild(0));
                //    drawnBricks.Add(cube);
                //}
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
                frezz = false;
                debugMode = false;
                // smallPenguinPos = new Vector3(10, 10, 10);
                // pointDevider.transform.position = brick.worldPos;
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
                minDistHeadToSpwanpoint = 0.7f;
                maxDistHeadToSpwanpoint = 1f;
                pointSide = PointSide.Both;
                Debug.LogError("Human detected: Slice: Random - Max Size: 4 - Min Size: 2");
            }
            else if (brick.labelName == "sheep" && !debugMode)
            {
                //One big stack
                maxStackSize = 8;
                minStackSize = 4;
                sliceMethod = SliceMethod.Max;
                minDistHeadToSpwanpoint = 0.0f;
                maxDistHeadToSpwanpoint = 0.4f;
                pointSide = PointSide.Both;
                Debug.LogError("Sheep detected: Slice: Max - Max Size: 8 - Min Size: 4");
            }
            else if (brick.labelName == "pig" && !debugMode)
            {
                //many small stacks
                maxStackSize = 3;
                minStackSize = 1;
                sliceMethod = SliceMethod.Random;
                minDistHeadToSpwanpoint = 0.55f;
                maxDistHeadToSpwanpoint = 0.75f;
                pointSide = PointSide.Right;
                Debug.LogError("Pig detected: Slice: Random - Max Size: 3 - Min Size: 1");
            }
            else if (brick.labelName == "lion")
            {
                maxStackSize = 4;
                minStackSize = 1;
                sliceMethod = SliceMethod.Min;
                minDistHeadToSpwanpoint = 0.3f;
                maxDistHeadToSpwanpoint = 0.6f;
                pointSide = PointSide.Left;
                Debug.LogError("Lion detected: Slice: Min - Max Size: 4 - Min Size: 2");
            }
        });

        if (prevPenguin != currPenguin)
        {
            prevPenguin = currPenguin;
            //frezz = true;
        }
        else
        {
            currPenguin = 0;
            prevPenguin = 0;
        }


        //newDectetion = true;
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

        if (state == GameState.Play && !frezz)
        {
            //CalculateStacks();
            CalculateStacksFromSpawnpoints();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (frezz)
        {
            return;
        }

        //ClearText();
        if (runOnce)
        {
            GetRoom();
        }

        if (bricks.Count >= 0)
        {
            rectPos1.SetActive(debugMode);
            rectPos2.SetActive(debugMode);
            rectCenter.SetActive(debugMode);

            if (debugMode)
            {
                //DebugMenu();
                RunDebugMenu();
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
            makeNewLevel = true;
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

        if (levelReset) return;
        //GameUtils.AddText(centerCam, canvas, playText, displayPos + new Vector3(0, 0.15f, 0), Color.white, 3);
        mainText.transform.position = displayPos + new Vector3(0, 0.15f, 0);
        //mainText.transform.GetComponentInChildren<TMP_Text>().text = playText + $"\nLevels Completed: {levelsComplteded}";
        mainText.transform.GetChild(0).LookAt(centerCam);
        mainText.transform.GetChild(0).Rotate(Vector3.up, 180);
        
        //DestroyCubes(1);

        if (makeNewLevel)
        {
            DestroyCubes(0);
            NewTable();
            //state = "seprate";
        }
        else
        {
            taskComplet = CheckIfAllDone();
            if (taskComplet && !levelReset)
            {
                levelsComplteded++;
                mainText.GetComponentInChildren<TMP_Text>().text = "Opgave løst.\nGood gjort :)";
                StartCoroutine(WaitForTaskComplete());
            }
        }
    }

    private void CalculateStacksFromSpawnpoints()
    {
        mainText.transform.GetComponentInChildren<TMP_Text>().text = playText + $"\nNiveauer gennemført: {levelsComplteded}";
        DestroyCubes(1);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            //stacksInFrame = FindStacksInImage(bricks, variableThreshold[stacksToBuild[i].Count-1]);

            if (stacksInFrame.Count == 0)
            {
                continue;
            }

            if (!spawnPoints[i]) continue;
            
            //DrawDebugStacks(stacksInFrame, 0.1f * i);

            //Finder distance til alle stacks
            float[] distTostacks = new float[stacksInFrame.Count];
            for (int j = 0; j < distTostacks.Length; j++)
            {
                distTostacks[j] = Vector3.Distance(spawnPoints[i].position, stacksInFrame[j][0].worldPos);
            }

            //Dinder tï¿½teste stack
            int idx = 0;
            float min = 1000;
            for (int j = 0; j < distTostacks.Length; j++)
            {
                if (distTostacks[j] < min)
                {
                    min = distTostacks[j];
                    idx = j;
                }
            }

            if (distTostacks[idx] < distToPointThreshold)
            {
                spawnpointNoDetectionCounts[i] = 0;

                //sikker at hï¿½jere stacks er hï¿½j nok
                //bool temp = false;
                //float hight = 0;
                //int id = -1;
                //for (int k = 0; k < stacksInFrame[idx].Count; k++)
                //{
                //    if (stacksInFrame[idx][k].worldPos.y > hight)
                //    {
                //        hight = stacksInFrame[idx][k].worldPos.y;
                //        id = k;
                //    }
                //}
                //if (stacksToBuild[i].Count > 2)
                //{
                //    temp = hight > (stacksToBuild[i].Count / 100f) + tableAnchor.transform.position.y;
                //}
                //else
                //{
                //    temp = true;
                //}
               

                //mainText.transform.GetComponentInChildren<TMP_Text>().text += $"\nTarget: {(stacksToBuild[i].Count / 100f) + tableAnchor.transform.position.y} | Highest brick: {hight} | Bool: {temp}";

                List<string> placedStack = GameUtils.DetectedObjectListToStringList(stacksInFrame[idx]);

                if (GameUtils.HaveSameElementAtSameIndex(stacksToBuild[i], placedStack)|| complted[i])
                {
                    spawnpointRightStackCounts[i]++;
                    if (spawnpointRightStackCounts[i] > 0 || complted[i]) //1
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
                        spawnpointRightStackCounts[i] = 2;
                    }
                    spawnpointWrongStackCounts[i] = 0;
                }
                else
                {
                    spawnpointWrongStackCounts[i]++;
                    if (spawnpointWrongStackCounts[i] > 2)//4
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

                        if (spawnpointWrongStackCounts[i] > 10)
                        {
                            spawnpointRightStackCounts[i]--;
                            spawnpointWrongStackCounts[i] = 5;
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
            //Debug text 
            //spawnPoints[i].GetComponentInChildren<TMP_Text>().text =
            //    $"Green: {spawnpointRightStackCounts[i]}\nRed: {spawnpointWrongStackCounts[i]}\nWhite: {spawnpointNoDetectionCounts[i]}";


        }
    }

    private void CalculateStacks()
    {

        //stacksInFrame = FindStacksInFrame(bricks);

        //if (stacksInFrame.Count == 0)
        //{
        //    return;
        //}

        //stacksInFrame = FindStacksInFrame(FixStacks(stacksInFrame, bricks));

        stacksInFrame = FindStacksInImage(bricks, stackThreshold);

        if (stacksInFrame.Count == 0)
        {
            return;
        }

        DestroyCubes(1);
        //for (int i = 0; i < stacksInFrame.Count; i++)
        //{
        //    for (int j = 0; j < stacksInFrame[i].Count; j++)
        //    {
        //        GameObject cube = Instantiate(GameManager.Instance.GetBrick(stacksInFrame[i][j].labelName), stacksInFrame[i][j].worldPos, Quaternion.identity, cubeParent.transform.GetChild(1));
        //    }
        //}

       

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
            if (distMat[i, ints[i]] < distToPointThreshold)
            {
                spawnpointNoDetectionCounts[i] = 0;

                List<string> placedStack = GameUtils.DetectedObjectListToStringList(stacksInFrame[ints[i]]);

                if (GameUtils.HaveSameElementAtSameIndex(stacksToBuild[i], placedStack) || complted[i])
                {
                    spawnpointRightStackCounts[i]++;
                    if (spawnpointRightStackCounts[i] > 1 || complted[i])
                    {
                        if (!complted[i])
                        {
                            var particleSystems = spawnPoints[i].GetComponentsInChildren<ParticleSystem>();

                            foreach (var system in particleSystems)
                            {
                                system.Play();
                            }
                            DataLogger.Log($"StackGeneration",$"COMPLETED;{i}");
                        }
                        var col = Color.green;
                        col.a = 0.33f;
                        spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = col;
                        complted[i] = true;
                        spawnpointRightStackCounts[i] = 2;
                    }
                    spawnpointWrongStackCounts[i] = 0;
                }
                else
                {
                    spawnpointWrongStackCounts[i]++;
                    if (spawnpointWrongStackCounts[i] > 2)
                    {
                        var col = Color.red;
                        col.a = 0.33f;
                        spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = col;
                        //spawnpointRightStackCounts[i]--;
                        //if (spawnpointRightStackCounts[i] < 0) { spawnpointRightStackCounts[i] = 0;}
                        //spawnpointWrongStackCounts[i] = 3;
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
                else if (spawnpointNoDetectionCounts[i] > 2)
                {
                    var col = Color.white;
                    col.a = 0.33f;
                    spawnPoints[i].GetChild(0).GetComponent<Renderer>().material.color = col;
                    //spawnpointNoDetectionCounts[i] = 3;
                    //spawnpointWrongStackCounts[i] = 0;
                    //spawnpointRightStackCounts[i] = 0;
                }
            }
            //Debug text
            spawnPoints[i].GetComponentInChildren<TMP_Text>().text =
                $"Green: {spawnpointRightStackCounts[i]}\nRed: {spawnpointWrongStackCounts[i]}\nWhite: {spawnpointNoDetectionCounts[i]}";

            taskComplet = CheckIfAllDone();
            if (taskComplet && !levelReset)
            {
                levelsComplteded++;
                DataLogger.Log($"StackGeneration","FINISHED");
                mainText.GetComponentInChildren<TMP_Text>().text = "Task completed!\nGoodjob :)";
                StartCoroutine(WaitForTaskComplete());
            }
        }
    }

    IEnumerator WaitForTaskComplete()
    {
        levelReset = true;
        yield return new WaitForSeconds(2);
        makeNewLevel = true;
        levelReset = false;
    }

    private List<List<DetectedObject>> FindStacksInImage(List<DetectedObject> detectedBricks, float threshold)
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

        int[,] ids = GameUtils.ClosestBricks(distArr, threshold);

        List<List<int>> stacks = GameUtils.FindConnectedComponents(ids);

        

        foreach (var stack in stacks)
        {
            stack.Sort((a, b) => detectedBricks[a].screenPos.y.CompareTo(detectedBricks[b].screenPos.y));

            var stackColorRow = stack.Select(t => detectedBricks[t]).ToList();

            stacksColor.Add(stackColorRow);
        }
        return stacksColor;
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
                CalibratePosition();
                break;

            case 2:
                showAllPoints = !showAllPoints;
                break;

            case 3:
                for (int i = 0; i < complted.Length; i++)
                {
                    complted[i] = true;
                }
                break;

            case 4:
                float minX = Mathf.Min(rectPos1.transform.localPosition.x, rectPos2.transform.localPosition.x);
                float maxX = Mathf.Max(rectPos1.transform.localPosition.x, rectPos2.transform.localPosition.x);
                float minY = Mathf.Min(rectPos1.transform.localPosition.y, rectPos2.transform.localPosition.y);
                float maxY = Mathf.Max(rectPos1.transform.localPosition.y, rectPos2.transform.localPosition.y);
                ourPlaneRect = new Rect(minX, minY, maxX - minX, maxY - minY);
                UpdateRectDispaly(ourPlaneRect);
                planeCenter = Vector3.Lerp(rectPos1.transform.position, rectPos2.transform.position, 0.5f);
                rectCenter.transform.position = planeCenter;
                break;

            case 5:
                ourPlaneRect = (Rect)tableAnchor.PlaneRect;
                UpdateRectDispaly(ourPlaneRect);
                rectCenter.transform.position = tableAnchor.transform.position;
                break;

            case 6:
                debugHand.SetActive(!debugHand.activeSelf);
                break;

            default:
                break;
        }
        adminAction = -1;
    }

    private void UpdateDebugText()
    {
        string[] texts = new string[7] {
                         "Debug mode \"On\". \nTo disable detect \"big penguin\".",
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

        GameObject recalibrater = GameUtils.MakeInteractionCirkle(menuPos + xOffset * -2, Color.blue);
        recalibrater.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Recalibrate" + showAllPoints, menuPos + xOffset * -2 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject pointShower = GameUtils.MakeInteractionCirkle(menuPos + xOffset * -1, Color.blue);
        pointShower.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Show all points: " + showAllPoints, menuPos + xOffset * -1 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject completTask = GameUtils.MakeInteractionCirkle(menuPos + xOffset * 0, Color.blue);
        completTask.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Complet task", menuPos + xOffset * 0 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject makeNewRect = GameUtils.MakeInteractionCirkle(menuPos + xOffset * 1, Color.blue);
        makeNewRect.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Make new rect", menuPos + xOffset * 1 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject resteRect = GameUtils.MakeInteractionCirkle(menuPos + xOffset * 2, Color.blue);
        resteRect.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Reset rect", menuPos + xOffset * 2 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

        GameObject handDispaly = GameUtils.MakeInteractionCirkle(menuPos + xOffset * 3, Color.blue);
        handDispaly.transform.parent = parent.transform;
        debugTestObjects.Add(GameUtils.AddText("Hand display: " + debugHand.activeSelf, menuPos + xOffset * 3 + new Vector3(0, 0.01f, 0), Color.white, 0.8f));

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
            LevelProgress();
    
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


            stacksToBuild = StackGenerator.GenerateStacks(briksToUse, minStackSize, maxStackSize, sliceMethod);

            //Turtoial stuf
            if (levelsComplteded == 0)
            {
                stacksToBuild = turtoial1;
            }
            else if (levelsComplteded == 1)
            {
                stacksToBuild = turtoial2;
            }

            // Random punkter pï¿½ hele boret
            //spawnPoints = GameUtils.DiskSampledSpawnPoints(tableAnchor, stacksToBuild.Count, spawnPositions.transform, ourPlaneRect);

            // Random punkter med min og max distands til head
            spawnPoints = GameUtils.DiskSampledSpawnPoints(tableAnchor, stacksToBuild.Count, spawnPositions.transform, ourPlaneRect, centerCam, minDistHeadToSpwanpoint, maxDistHeadToSpwanpoint, pointSide, showAllPoints);


            for (int i = 0; i < stacksToBuild.Count; i++)
            {
                List<GameObject> tempStack = GameUtils.DrawStack(stacksToBuild[i], spawnPoints[i].position + new Vector3(0,0.025f,0) + offsetDir * 0.07f);
                foreach (GameObject item in tempStack)
                {
                    item.transform.parent = cubeParent.transform.GetChild(0);
                }
                GameObject cirkel = GameUtils.MakeInteractionCirkle(spawnPoints[i].position + new Vector3(0,0.015f,0), Color.white);
                cirkel.transform.localScale = new Vector3(0.08f, 0.001f, 0.08f);
                cirkel.transform.parent = spawnPoints[i];
                
                DataLogger.Log($"StackGeneration",$"GENERATE;{i};{DataCollection.GetPlaneNormalizedCoordinates(spawnPoints[i].position).ToString("F5")}");

                //Debug text
                GameObject text = GameUtils.AddText("", spawnPoints[i].position + new Vector3(0, 0.06f, 0), Color.white);
                text.transform.parent = cirkel.transform;
            }


            dists = new float[stacksToBuild.Count];
            complted = new bool[stacksToBuild.Count];
            spawnpointNoDetectionCounts = new int[stacksToBuild.Count];
            spawnpointWrongStackCounts = new int[stacksToBuild.Count];
            spawnpointWrongOrderCounts = new int[stacksToBuild.Count];
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
            case 2:
            case 3:
                maxNumberOfBriksToUse = 2;
                minStackSize = 1;
                maxStackSize = 2;
                sliceMethod = SliceMethod.Min;
                minDistHeadToSpwanpoint = 0.2f;
                maxDistHeadToSpwanpoint = 0.6f;
                break;

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
        // Tjecks if the displayAnchor is in the rect
        if (Vector3.Dot(centerCam.forward, displayAnchor) < 0)
        {
            offsetDir = -offsetDir;
        }
        //GameUtils.MakeInteractionCirkle(displayAnchor + offsetDir * 0.70f, Color.red);

        //offsetDir = (new Vector3(anchorPoint.x, 0, anchorPoint.z) - new Vector3(centerCam.position.x, 0, centerCam.position.z)).normalized;

        //displayPos = anchorPoint + offsetDir * 0.25f;
        displayPos = displayAnchor + offsetDir * 0.65f;


        mainText.transform.position = displayPos + new Vector3(0, 0.15f, 0);

        rectPos1.transform.parent = tableAnchor.transform;

        rectPos2.transform.parent = tableAnchor.transform;

        rectCenter.transform.position = tableAnchor.transform.position;

        //debugMenu.transform.position = tableAnchor.transform.position + offsetDir * -0.2f;

        //debugHand.transform.position = tableAnchor.transform.position + offsetDir * 0.4f;
        //debugHand.transform.Rotate(offsetDir, -90);
        //debugHand.SetActive(false);

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
    Separate
}

public enum PointSide
{
    Right,
    Left,
    Both
}

