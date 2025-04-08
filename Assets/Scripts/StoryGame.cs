using Meta.XR.MRUtilityKit;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StoryGame : MonoBehaviour
{
    public ObjectDetector objectDetection;

    public GameObject spawnPositions;
    public GameObject cubeParent;
    public GameObject canvas;

    public GameObject cylinderPrefab;

    public Transform centerCam;

    private FindSpawnPositions _findSpawnPositions;

    public float distans = 0.05f;

    bool taskComplet = false;

    Dictionary<string, int> bricksInFrame = new Dictionary<string, int>();
    Dictionary<string, int> briksToBuildStack = new Dictionary<string, int> { { "red", 1 }, { "green", 1 }, { "blue", 1 }, { "yellow", 1 }, { "magenta", 0 } };

    string state = "setup";

    List<GameObject> drawnBricks = new List<GameObject>();

    float[] distArr = new float[3] { 100, 100, 100};
    bool[] visits = new bool[3] { false, false, false };
    string[] colors = {"green", "blue", "yellow" };
    string playerColor = "red";

    MRUKRoom room;
    List<MRUKAnchor> anchors = new();

    Vector3 displayPos = new Vector3();
    Vector3 debugDisplayPos = new Vector3();
    Vector3 displayOfset = new Vector3(0, 0, -0.05f);

    private bool runOnce = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _findSpawnPositions = spawnPositions.GetComponent<FindSpawnPositions>();
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
        else if (state == "play")
        {
            Play();
        }
    }

    private void Setup()
    {
        List<Brick> bricks = objectDetection.GetBricks();
        ResetBricksInFrame();

        drawnBricks.ForEach(Destroy);
        drawnBricks.Clear();

        foreach (Transform t in canvas.transform)
        {
            Destroy(t.gameObject);
        }

        foreach (var brick in bricks)
        {
            bricksInFrame[brick.colorName]++;
            GameObject cube = brick.DrawSmall();
            AddText(brick.worldPos.ToString(), brick.worldPos, GameUtils.nameToColor[brick.colorName]);
            drawnBricks.Add(cube);
        }

        if ((bricksInFrame["red"] == 1 && bricksInFrame["green"] == 1 && bricksInFrame["blue"] == 1 && bricksInFrame["yellow"] == 1))// || bricks.Count == 4)
        {

            for (int i = 0; i < distArr.Length; i++)
            {
                Brick b = GetBrickWithColor(bricks, colors[i]);
                GameObject cylinder = Instantiate(cylinderPrefab, b.worldPos + displayOfset, Quaternion.identity, cubeParent.transform);
                cylinder.GetComponent<Renderer>().material.color = Color.white;
            }
            distArr = new float[3] { 100, 100, 100 };
            taskComplet = false;
            
            drawnBricks.ForEach(Destroy);
            drawnBricks.Clear();
            state = "play";
        }
    }

    private void Play()
    {
        List<Brick> bricks = objectDetection.GetBricks();
        ResetBricksInFrame();
        drawnBricks.ForEach(Destroy);
        drawnBricks.Clear();

        string text = "Test text";

        foreach (Transform t in canvas.transform)
        {
            Destroy(t.gameObject);
        }

        foreach (var brick in bricks)
        {
            if (brick.colorName != "red")
            {
                bricksInFrame[brick.colorName]++;
                GameObject cube = brick.DrawSmall();
                AddText(brick.colorName + " brick", brick.worldPos, GameUtils.nameToColor[brick.colorName]);
                drawnBricks.Add(cube);
            }
        }

        if (!CheckIfAllVisited())
        {
            text = "Talk to all the characters";
        }

        Brick player = GetBrickWithColor(bricks, playerColor);
        if (player != null)
        {
            GameObject cubePlayer = player.DrawSmall();
            AddText("Player", player.worldPos, GameUtils.nameToColor[player.colorName]);
            drawnBricks.Add(cubePlayer);

            //If the task is completed, choose new colors
            if (taskComplet)
            {
                AddText("Game done", displayPos, Color.white, 2f);
            }
            else
            {
                for (int i = 0; i < distArr.Length; i++)
                {
                    Brick b = GetBrickWithColor(bricks, colors[i]);
                    if (b != null)
                    {
                        float dist = Vector3.Distance(b.worldPos + displayOfset, player.worldPos);
                        distArr[i] = dist;

                        if (distArr[i] < distans)
                        {
                            visits[i] = true;
                            cubeParent.transform.GetChild(i).gameObject.GetComponent<Renderer>().material.color = Color.magenta;
                        }
                    }

                    if (visits[i] && distArr[i] > distans)
                    {
                        cubeParent.transform.GetChild(i).gameObject.GetComponent<Renderer>().material.color = Color.cyan;
                    }
                }

                //Get id of lowest distance
                int minIndex = 0;
                float minValue = distArr[0];
                for (int i = 1; i < distArr.Length; i++)
                {
                    if (distArr[i] < minValue)
                    {
                        minValue = distArr[i];
                        minIndex = i;
                    }
                }
                if (minValue < distans)
                {
                    text = "Talk to " + colors[minIndex] + " brick";
                }

                AddText(text, displayPos, Color.white, 2f);

                if (CheckIfAllVisited())
                {
                    DestroyCubes();
                    taskComplet = true;
                    visits = new bool[3] { false, false, false };
                    distArr = new float[3] { 100, 100, 100 };
                }
            }
        }


    }

    private bool CheckIfAllVisited()
    {
        foreach (bool b in visits)
        {
            if (!b)
            {
                return false;
            }
        }
        return true;
    }

    public void AddText(string text, Vector3 position, Color color, float fontsize = 1)
    {
        // Create a new GameObject for the text and set its attributes.
        GameObject newGameObject = new GameObject();
        RectTransform rect = newGameObject.AddComponent<RectTransform>();
        rect.position = position + new Vector3(0, 0.03f, 0);
        rect.rotation = Quaternion.identity;
        rect.LookAt(centerCam);
        rect.Rotate(Vector3.up, 180);
        rect.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        newGameObject.transform.SetParent(canvas.transform);
        TextMeshPro newText = newGameObject.AddComponent<TextMeshPro>();

        // Set specific TextMeshPro settings, extend this as you see fit.
        newText.text = text;
        newText.fontSize = fontsize;
        newText.alignment = TextAlignmentOptions.Center;
        newText.color = color;

    }

    private void DestroyCubes()
    {
        foreach (Transform item in cubeParent.transform)
        {
            Destroy(item.gameObject);
        }
    }

    private Brick GetBrickWithColor(List<Brick> bricks, string color)
    {
        foreach (var brick in bricks)
        {
            if (brick.colorName == color)
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

    private void GetRoom()
    {
        room = MRUK.Instance.GetCurrentRoom();
        Vector3 anchorPoint = new Vector3(0, 0, 0);
        Debug.LogWarning(room != null);
        if (room != null)
        {
            anchors = room.Anchors;
            foreach (MRUKAnchor anchor in anchors)
            {
                if (anchor.Label == MRUKAnchor.SceneLabels.TABLE)
                {
                    anchorPoint = anchor.gameObject.transform.position;
                    // displayPos.rotation = Quaternion.Euler(anchor.gameObject.transform.localRotation.eulerAngles + new Vector3(-90, 0, -180));
                }
            }
            runOnce = false;
        }

        if (!runOnce)
        {
            Vector3 offsetDir = (anchorPoint - new Vector3(centerCam.position.x, anchorPoint.y, centerCam.position.z)).normalized;

            displayPos = anchorPoint + offsetDir * 0.2f;
 
            debugDisplayPos = displayPos + new Vector3(0.2f, 0, 0);

            displayOfset = offsetDir * -0.05f;

        }
    }
}
