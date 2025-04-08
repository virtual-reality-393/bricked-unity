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

    bool taskComplet = true;

    Dictionary<string, int> bricksInFrame = new Dictionary<string, int>();
    Dictionary<string, int> briksToBuildStack = new Dictionary<string, int> { { "red", 1 }, { "green", 1 }, { "blue", 1 }, { "yellow", 1 }, { "magenta", 0 } };

    List<string> stackToBuild = new List<string>();

    string state = "setup";

    List<GameObject> drawnBricks = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _findSpawnPositions = spawnPositions.GetComponent<FindSpawnPositions>();
    }

    // Update is called once per frame
    void Update()
    {
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
            AddText(brick.worldPos.ToString(), brick.worldPos, GameUtils.nameToColor[brick.labelName]);
            drawnBricks.Add(cube);

        }

        if ((bricksInFrame["red"] == 1 && bricksInFrame["green"] == 1 && bricksInFrame["blue"] == 1 && bricksInFrame["yellow"] == 1))// || bricks.Count == 4)
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
            AddText(brick.worldPos.ToString(), brick.worldPos, GameUtils.nameToColor[brick.labelName]);
            drawnBricks.Add(cube);

        }
        DestroyCubes();

        //If the task is completed, choose new colors
        if (taskComplet)
        {
            //Make stack to build and place it on table
            stackToBuild = GameUtils.GenetateStack(briksToBuildStack);
            _findSpawnPositions.StartSpawn();
            Vector3 pos = spawnPositions.transform.GetChild(0).transform.position;

            for (int i = 0; i < stackToBuild.Count; i++)
            {
                GameObject cube = Instantiate(GameManager.Instance.brickPrefab, pos + new Vector3(0,0.03f,0) * i, Quaternion.identity, spawnPositions.transform);
                cube.GetComponent<Renderer>().material.color = GameUtils.GetColorByName(stackToBuild[i]);
            }

            taskComplet = false;
        }
        else
        {
            AddText("Stact to build", spawnPositions.transform.GetChild(0).transform.position + new Vector3(0, 0.03f, 0) * stackToBuild.Count, Color.white);
            //Tjeck stacks in frame    
            List<string> bricksColorSorted = SortGameObjectsByY(bricks);

            Vector3 pos = spawnPositions.transform.GetChild(1).transform.position;
            for (int i = 0; i < bricksColorSorted.Count; i++)
            {
                GameObject cube = Instantiate(GameManager.Instance.brickPrefab, pos + new Vector3(0, 0.03f, 0) * i, Quaternion.identity, cubeParent.transform);
                cube.GetComponent<Renderer>().material.color = GameUtils.GetColorByName(bricksColorSorted[i]);
            }
            AddText("Curret stack", pos + new Vector3(0, 0.03f, 0) * bricksColorSorted.Count, Color.white);

            bool b = false;
            if (stackToBuild.Count == bricksColorSorted.Count)
            {
                b = true;
                for (int i = 0; i < stackToBuild.Count; i++)
                {
                    b = b && stackToBuild[i] == bricksColorSorted[i];
                    if (!b)
                    {
                        break;
                    }
                }
            }

            if (b || bricks.Count == 6)
            {
                DestroySpawnPositions();
                taskComplet = true;
            }


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
    private void DestroyCubes()
    {
        foreach (Transform item in cubeParent.transform)
        {
            Destroy(item.gameObject);
        }
    }
    private DetectedObject GetBrickWithColor(List<DetectedObject> bricks, string color)
    {
        foreach (var brick in bricks)
        {
            if (brick.labelName == color)
            {
                return brick;
            }
        }
        return null;
    }

    private void ResetBricksInFrame()
    {
        bricksInFrame = new Dictionary<string, int> { { "red", 0 }, { "green", 0 }, { "blue", 0 }, { "yellow", 0 }, { "magenta", 0 } };
    }


    // Method to add a text to the UI with specified attributes.
    public void AddText(string text, Vector3 position, Color color)
    {
        // Create a new GameObject for the text and set its attributes.
        GameObject newGameObject = new GameObject();
        RectTransform rect = newGameObject.AddComponent<RectTransform>();
        rect.position = position + new Vector3(0,0.03f, 0);
        rect.rotation = Quaternion.identity;
        rect.LookAt(centerCam);
        rect.Rotate(Vector3.up, 180);
        rect.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        newGameObject.transform.SetParent(canvas.transform);
        TextMeshPro newText = newGameObject.AddComponent<TextMeshPro>();

        // Set specific TextMeshPro settings, extend this as you see fit.
        newText.text = text;
        newText.fontSize = 1;
        newText.alignment = TextAlignmentOptions.Center;
        newText.color = color;
    }
}
