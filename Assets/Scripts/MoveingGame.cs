using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MoveingGame : MonoBehaviour
{
    public ObjectDetection objectDetection;

    public float Distans = 0.2f;

    private Brick target;
    private Brick toMove;


    bool taskComplet = true;

    Dictionary<string, int> bricksInFrame = new Dictionary<string, int>();

    string state = "setup";

    string targetColor = "red";
    string toMoveColor = "green";

    List<GameObject> drawnBricks = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bricksInFrame = new Dictionary<string, int>{{ "red", 0 },{ "green", 0 },{ "blue", 0 },{ "yellow", 0 }};

        // Initialize LineRenderer
        //lineRenderer = gameObject.AddComponent<LineRenderer>();
        //lineRenderer.startWidth = 0.05f;
        //lineRenderer.endWidth = 0.05f;
        //lineRenderer.positionCount = 2;
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
        List<Brick> bricks = objectDetection.bricks;
        ResetBricksInFrame();

        drawnBricks.ForEach(Destroy);
        drawnBricks.Clear();

        foreach (var brick in bricks)
        {
            bricksInFrame[brick.colorName]++;
            GameObject cube = brick.Draw(Color.white);
            drawnBricks.Add(cube);
        }

        if ((bricksInFrame["red"] == 1 && bricksInFrame["green"] == 1 && bricksInFrame["blue"] == 1 && bricksInFrame["yellow"] == 1) || bricks.Count == 4)
        {
            state = "play";
        }
    }
    string tempColor1 = "red";
    string tempColor2 = "green";
    private void Play()
    {
        List<Brick> bricks = objectDetection.bricks;
        string[] colors = { "red", "green", "blue", "yellow" };

        // If the task is completed, choose new colors
        //if (taskComplet)
        //{
        //    System.Random random = new();
        //    int targetColorIndex = random.Next(0, colors.Length);
        //    int toMoveColorIndex = random.Next(0, colors.Length);

        //    if (targetColorIndex == toMoveColorIndex)
        //    {
        //        if (targetColorIndex == 1)
        //        {
        //            toMoveColorIndex = 0;
        //        }
        //        else
        //        {
        //            toMoveColorIndex = 1;
        //        }
        //    }

        //    targetColor = colors[targetColorIndex];
        //    toMoveColor = colors[toMoveColorIndex];

        //    taskComplet = false;
        //}

        target = GetBrickWithColor(bricks, tempColor1);
        toMove = GetBrickWithColor(bricks, tempColor2);

        // clear old visualization and make new
        drawnBricks.ForEach(Destroy);
        drawnBricks.Clear();

        foreach (var brick in bricks)
        {
            GameObject cube = brick.Draw();
            drawnBricks.Add(cube);
        }
        //if (target != null)
        //{
        //    GameObject targetBrick = target.Draw(Color.magenta);
        //    drawnBricks.Add(targetBrick);
        //}

        //if (toMove != null)
        //{
        //    GameObject toMoveBrick = toMove.Draw(Color.cyan);
        //    drawnBricks.Add(toMoveBrick);
        //}


        // Draw a line between target and toMove
        //lineRenderer.SetPosition(0, target.worldPos);
        //lineRenderer.SetPosition(1, toMove.worldPos);
        //lineRenderer.startColor = lineRenderer.endColor = Vector3.Distance(target.worldPos, toMove.worldPos) <= Distans ? Color.green : Color.red;

        // tjek if the task is completed
        if (target != null && toMove != null)
        {
            if (Vector3.Distance(target.worldPos, toMove.worldPos) <= Distans)
            {
                taskComplet = false;
                tempColor1 = "blue";
                tempColor2 = "yellow";
            }
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

    private float[,] DistMat(List<Brick> bricks)
    {
        float[,] distArr = new float[bricks.Count, bricks.Count];
        for (int i = 0; i < bricks.Count; i++)
        {
            for (int j = 0; j < bricks.Count; j++)
            {
                if (i == j)
                {
                    distArr[i, j] = 0;
                }
                else
                {
                    distArr[i, j] = Vector3.Distance(bricks[i].worldPos, bricks[j].worldPos);
                }
            }
        }
        return distArr;
    }

    private void ResetBricksInFrame()
    {
        bricksInFrame = new Dictionary<string, int> { { "red", 0 }, { "green", 0 }, { "blue", 0 }, { "yellow", 0 } };
    }
}
