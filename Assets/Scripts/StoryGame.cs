using Meta.XR.MRUtilityKit;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class StoryGame : MonoBehaviour
{
    public ObjectDetector objectDetection;

    public GameObject spawnPositions;
    public GameObject cubeParent;
    public GameObject canvas;

    public Transform centerCam;

    public float theshold = 0.05f;

    Dictionary<string, int> bricksInFrame = new Dictionary<string, int>();
   
    string state = "setup";

    List<GameObject> drawnObjects = new List<GameObject>();

    float[] distArr = new float[3] { 100, 100, 100};
    bool[] visits = new bool[3] { false, false, false };

    string[] objectsToDetect = {"red", "green", "blue", "yellow", "big penguin", "small penguin", "pig", "human" };
    string[] interactables = { "red","green", "blue", "yellow", "big penguin", "pig", "human" };
    string playerColor = "small penguin";
    List<DetectedObject> objects = new List<DetectedObject>();

    MRUKRoom room;
    List<MRUKAnchor> anchors = new();

    Vector3 anchorPoint = new Vector3(0, 0, 0);
    Vector3 offsetDir = new Vector3(0, 0, 0);
    Vector3 displayPos = new Vector3();
    Vector3 displayOfset = new Vector3(0, 0, -0.05f);

    private bool runOnce = true;

    DetectedObject whereDid = new DetectedObject();
    DetectedObject whoDid = new DetectedObject();

    Dictionary<string,string> stroy = new Dictionary<string, string>
    {
        {"red", "What is this? \nA \"KNIFE !!\". Could this be the murder weapon?"},
        {"green", "There are clear signs of struggle her, but no blod."},
        {"blue", "This is just a nice lake, it has no connection to the story."},
        {"yellow", "What's in here? \nit's a lot of blood and something else, something that's soft and covered in blood."},
        {"big penguin", "This is the victim, there are no \nobvious signs of how the murder was committed."},
        {"small penguin", "Hey I'm the player."},
        {"pig", "Dead!! big penguin is dead. I'm not sorry about that, \nbig penguin made a lot of noise tying to fly all the time, it was annoying."},
        {"human", "Big penguin was a nice one, the only annoying \nthing was the countless attempts to fly away."},
    };

    int ending = 0;
    Dictionary<int, string> endings = new Dictionary<int, string>
    {
        {0,"You are worng, start over and try again." },
        {1, "No human did not kill big penguin. \nHuman killed sheap (Thats way there are no sheap)." },
        {2, "You did it, it was the the pig all along, it wanted to fly befor the big penguin." },
        {3, "No the big penguin did not kill it self." }
    };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        objectDetection.OnObjectsDetected += HandleBricksDetected;

        distArr = new float[interactables.Length];
        visits = new bool[interactables.Length];
        for (int i = 0; i < distArr.Length; i++)
        {
            distArr[i] = 100;
            visits[i] = false;
        }
    }

    private void HandleBricksDetected(object sender, ObjectDetectedEventArgs e)
    {
        objects = new List<DetectedObject>();
        e.DetectedObjects.ForEach(obj =>
        {
            if (objectsToDetect.Contains(obj.labelName))
            {
                objects.Add(obj);
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
        else if (state == "play")
        {
            Play();
        }
        else if (state == "end")
        {
            End();
        }
        else if (state == "restart")
        {
            Restart();
        }
    }

    private void Setup()
    {
        ResetBricksInFrame();

        drawnObjects.ForEach(Destroy);
        drawnObjects.Clear();

        foreach (Transform t in canvas.transform)
        {
            Destroy(t.gameObject);
        }

        foreach (var obj in objects)
        {
            if (bricksInFrame.ContainsKey(obj.labelName))
            {
                bricksInFrame[obj.labelName]++;
            }
            GameObject cube = obj.DrawSmall();
            GameUtils.AddText(centerCam, canvas, obj.labelName, obj.worldPos, DetectedObject.labelToDrawColor[obj.labelIdx]);
            drawnObjects.Add(cube);
        }

        if ((bricksInFrame["red"] == 1 && bricksInFrame["green"] == 1 && bricksInFrame["blue"] == 1 && bricksInFrame["yellow"] == 1
          && bricksInFrame["big penguin"] == 1 && bricksInFrame["small penguin"] == 1 && bricksInFrame["pig"] == 1 && bricksInFrame["human"] == 1))// || bricks.Count == 4)
        {
            GameObject startCirkle = MakeInteractionCirkle(anchorPoint, Color.gray);
            drawnObjects.Add(startCirkle);
            GameUtils.AddText(centerCam, canvas, "Place player here to start the game", startCirkle.transform.position + new Vector3(0, 0.05f, 0), Color.white, 1.5f);

            DetectedObject player = GameUtils.GetBrickWithColor(objects, playerColor);
            if (Vector3.Distance(startCirkle.transform.position, player.worldPos) < theshold)
            {
                drawnObjects.ForEach(Destroy);
                drawnObjects.Clear();
                state = "play";
            }
        }
    }

    private void Play()
    {
        ResetBricksInFrame();
        drawnObjects.ForEach(Destroy);
        drawnObjects.Clear();
        foreach (var obj in objects)
        {
            if (bricksInFrame.ContainsKey(obj.labelName))
            {
                bricksInFrame[obj.labelName]++;
                GameObject cube = obj.DrawSmall();
                GameObject circle = MakeInteractionCirkle(obj.worldPos + offsetDir * -0.05f, Color.white);
                circle.transform.parent = cube.transform;
                drawnObjects.Add(cube);
            }
        }

        if ((bricksInFrame["red"] == 1 && bricksInFrame["green"] == 1 && bricksInFrame["blue"] == 1 && bricksInFrame["yellow"] == 1
            && bricksInFrame["big penguin"] == 1 && bricksInFrame["small penguin"] == 1 && bricksInFrame["pig"] == 1 && bricksInFrame["human"] == 1))// || bricks.Count == 4)
        {
            //Clear frame
            drawnObjects.ForEach(Destroy);
            drawnObjects.Clear();

            DestroyCubes();

            string text = "No player";

            foreach (Transform t in canvas.transform)
            {
                Destroy(t.gameObject);
            }
            //End clear frame


            if (!CheckIfAllVisited())
            {
                text = "Talk to all the characters and investigate the various places";
            }

            DetectedObject player = GameUtils.GetBrickWithColor(objects, playerColor);
            foreach (var obj in objects)
            {
                if (obj.labelName != playerColor)
                {
                    float dist = -1;
                    if (player != null)
                    {
                        dist = Vector3.Distance(obj.worldPos + displayOfset, player.worldPos);
                    }
                    GameObject cube = obj.DrawSmall();
                    GameUtils.AddText(centerCam, canvas, obj.labelName + " plyer dist: " + Math.Round(dist, 2), obj.worldPos, DetectedObject.labelToDrawColor[obj.labelIdx]);

                    Color color = Color.white;
                    int i = Array.IndexOf(interactables, obj.labelName);
                    visits[i] = visits[i] ? true : dist <= theshold;
                    if (visits[i] && dist <= theshold)
                    {
                        color = Color.magenta;
                        text = stroy[interactables[i]];
                    }
                    else if (visits[i] && dist > theshold)
                    {
                        color = Color.cyan;
                    }
                    GameObject circle = MakeInteractionCirkle(obj.worldPos + offsetDir * -0.05f, color);
                    circle.transform.parent = cube.transform;
                    cube.transform.parent = cubeParent.transform;
                    drawnObjects.Add(cube);
                    //drawnBricks.Add(circle);

                    //Debug code for skipping the investigating part bye only needing to talk to the "big penguin".
                    //if (visits[Array.IndexOf(interactables, "big penguin")])
                    //{
                    //    for (int j = 0; j < visits.Length; j++)
                    //    {
                    //        visits[j] = true;
                    //    }
                    //}
                }
            }

            if (CheckIfAllVisited())
            {
                GameObject nextCirkle = MakeInteractionCirkle(anchorPoint, Color.gray);
                nextCirkle.transform.localScale = new Vector3(0.05f, 0.005f, 0.05f);
                drawnObjects.Add(nextCirkle);
                GameUtils.AddText(centerCam, canvas, "Place player here to make accusation", nextCirkle.transform.position + new Vector3(0,0.05f,0), Color.white, 1.5f);

                if (Vector3.Distance(nextCirkle.transform.position, player.worldPos) < theshold)
                {
                    DestroyCubes();
                    state = "end";
                    distArr = new float[interactables.Length];
                    visits = new bool[interactables.Length];
                    for (int i = 0; i < distArr.Length; i++)
                    {
                        distArr[i] = 100;
                        visits[i] = false;
                    }
                }
            }
            
            GameUtils.AddText(centerCam, canvas, text, displayPos + new Vector3(0, 0.1f, 0), Color.white, 2f);
        }
    }

    private void End()
    {
        ResetBricksInFrame();
        drawnObjects.ForEach(Destroy);
        drawnObjects.Clear();
        foreach (Transform t in canvas.transform)
        {
            Destroy(t.gameObject);
        }

        foreach (var obj in objects)
        {
            if (bricksInFrame.ContainsKey(obj.labelName))
            {
                bricksInFrame[obj.labelName]++;
                GameObject cube = obj.DrawSmall();
                GameUtils.AddText(centerCam, canvas, obj.labelName, obj.worldPos, DetectedObject.labelToDrawColor[obj.labelIdx]);
                drawnObjects.Add(cube);
            }
        }

        if ((bricksInFrame["red"] == 1 && bricksInFrame["green"] == 1 && bricksInFrame["blue"] == 1 && bricksInFrame["yellow"] == 1
            && bricksInFrame["big penguin"] == 1 && bricksInFrame["small penguin"] == 1 && bricksInFrame["pig"] == 1 && bricksInFrame["human"] == 1))// || bricks.Count == 4)
        {
            drawnObjects.ForEach(Destroy);
            drawnObjects.Clear();

            foreach (Transform t in canvas.transform)
            {
                Destroy(t.gameObject);
            }

            GameUtils.AddText(centerCam, canvas, "Make your accusation", displayPos + new Vector3(0, 0.1f, 0), Color.white, 2f);

            GameObject circleWhere = MakeInteractionCirkle(anchorPoint + Vector3.Cross(offsetDir, Vector3.up) * 0.1f, Color.white);
            drawnObjects.Add(circleWhere);
            string text = "Where did it happen?";
            if (whereDid.labelName != "")
            {
                text += "\n" + whereDid.labelName;
            }
            GameUtils.AddText(centerCam, canvas, text, circleWhere.transform.position + new Vector3(0, 0.1f, 0), Color.white, 2f);
            float dist = 1000;
            for (int i = 0; i < 4; i++)
            {
                DetectedObject detectedObject = GameUtils.GetBrickWithColor(objects, interactables[i]);
                if (Vector3.Distance(circleWhere.transform.position, detectedObject.worldPos) < dist)
                {
                    dist = Vector3.Distance(circleWhere.transform.position, detectedObject.worldPos);
                    if (dist < theshold)
                    {
                        whereDid = detectedObject;
                    }
                    else
                    {
                        whereDid = new DetectedObject();
                    }
                }
            }

            GameObject circleWho = MakeInteractionCirkle(anchorPoint + Vector3.Cross(offsetDir, Vector3.up) * -0.1f, Color.white);
            drawnObjects.Add(circleWho);
            text = "Who did it?";
            if (whoDid.labelName != "")
            {
                text += "\n" + whoDid.labelName;
            }
            GameUtils.AddText(centerCam, canvas, text, circleWho.transform.position + new Vector3(0, 0.1f, 0), Color.white, 2f);
            dist = 1000;
            for (int i = 4; i < interactables.Length; i++)
            {
                DetectedObject detectedObject = GameUtils.GetBrickWithColor(objects, interactables[i]);
                if (Vector3.Distance(circleWho.transform.position, new Vector3(detectedObject.worldPos.x, circleWho.transform.position.y, detectedObject.worldPos.z)) < dist)
                {
                    dist = Vector3.Distance(circleWho.transform.position, detectedObject.worldPos);
                    if (dist < theshold)
                    {
                        whoDid = detectedObject;
                    }
                    else
                    {
                        whoDid = new DetectedObject();
                    }
                }
            }

            if (whereDid.labelName != "" && whoDid.labelName != "")
            {
                text = "";

                GameObject nextCirkle = MakeInteractionCirkle(anchorPoint + offsetDir * 0.15f, Color.gray);
                nextCirkle.transform.localScale = new Vector3(0.05f, 0.005f, 0.05f);
                drawnObjects.Add(nextCirkle);
                GameUtils.AddText(centerCam, canvas, "Place player to confirm accusation", nextCirkle.transform.position + new Vector3(0, 0.05f, 0), Color.white, 1.5f);

                DetectedObject player = GameUtils.GetBrickWithColor(objects, playerColor);
                if (Vector3.Distance(nextCirkle.transform.position, player.worldPos) < theshold)
                {
                    if (whereDid.labelName == "yellow" && whoDid.labelName == "human")
                    {
                        ending = 1;
                    }
                    else if (whereDid.labelName == "green" && whoDid.labelName == "pig")
                    {
                        ending = 2;
                    }
                    else if (whoDid.labelName == "big penguin")
                    {
                        ending = 3;
                    }
                    else
                    {
                        ending = 0;
                    }
                    state = "restart";
                }
            }
        }
    }

    private void Restart()
    {
        ResetBricksInFrame();
        drawnObjects.ForEach(Destroy);
        drawnObjects.Clear();
        foreach (Transform t in canvas.transform)
        {
            Destroy(t.gameObject);
        }

        GameUtils.AddText(centerCam, canvas, endings[ending], displayPos + new Vector3(0, 0.1f, 0), Color.white, 2f);

        foreach (var obj in objects)
        {
            if (bricksInFrame.ContainsKey(obj.labelName))
            {
                bricksInFrame[obj.labelName]++;
                GameObject cube = obj.DrawSmall();
                GameUtils.AddText(centerCam, canvas,obj.labelName, obj.worldPos, DetectedObject.labelToDrawColor[obj.labelIdx]);
                drawnObjects.Add(cube);
            }
        }

        GameObject nextCirkle = MakeInteractionCirkle(anchorPoint + offsetDir * -0.15f, Color.gray);
        nextCirkle.transform.localScale = new Vector3(0.05f, 0.005f, 0.05f);
        drawnObjects.Add(nextCirkle);
        GameUtils.AddText(centerCam, canvas, "Place player to restart game", nextCirkle.transform.position + new Vector3(0, 0.05f, 0), Color.white, 1.5f);

        if (bricksInFrame[playerColor] == 1)
        {
            DetectedObject player = GameUtils.GetBrickWithColor(objects, playerColor);
            if (Vector3.Distance(nextCirkle.transform.position, player.worldPos) < theshold)
            {
                state = "setup";
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

    private GameObject MakeInteractionCirkle(Vector3 pos, Color color)
    {
        GameObject circle = Instantiate(GameManager.Instance.cylinderPrefab, new Vector3(pos.x, displayPos.y-0.01f,pos.z), Quaternion.identity, cubeParent.transform);
        circle.GetComponent<Renderer>().material.color = color;
        circle.transform.localScale = new Vector3(0.05f, 0.005f, 0.05f);
        //circle.transform.parent = cubeParent.transform;
        return circle;
    }



    private void DestroyCubes()
    {
        foreach (Transform item in cubeParent.transform)
        {
            Destroy(item.gameObject);
        }
    }

    private void ResetBricksInFrame()
    {
        bricksInFrame = new Dictionary<string, int> { { "red", 0 }, { "green", 0 }, { "blue", 0 }, { "yellow", 0 } ,
                                                      { "big penguin", 0 }, { "small penguin", 0 }, { "pig", 0 } , {"human",0}};
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


