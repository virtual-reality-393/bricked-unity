using System;
using UnityEngine;

public class Brick
{
    public string colorName;
    public Color brickColor;
    public BoundingBox boundingBox;
    public Vector3 worldPos;
    
    public Brick(string colorName, Color color, BoundingBox boundingBox, Vector3 worldPos)
    {
        this.colorName = colorName;
        this.brickColor = color;
        this.boundingBox = boundingBox;
        this.worldPos = worldPos;
    }

    public Brick()
    {
        this.colorName = "";
        this.brickColor = Color.white;
        this.boundingBox = new BoundingBox();
        this.worldPos = Vector3.zero;
    }

    public GameObject Draw()
    {
        var cube = GameObject.Instantiate(GameManager.Instance.brickPrefab , worldPos, Quaternion.identity);
        cube.GetComponent<Renderer>().material.color = brickColor;
        return cube;
    }

    public GameObject Draw(Color color)
    {
        var cube = GameObject.Instantiate(GameManager.Instance.brickPrefab, worldPos, Quaternion.identity);
        cube.GetComponent<Renderer>().material.color = color;
        return cube;
    }
}


[Serializable]
public struct PythonBrick
{
    public string color;
    //public Color brickColor;
    public int[] box;
    public int[] center;

    public PythonBrick(string color, int[] box, int[] center)
    {
        this.color = color;
        this.box = box;
        this.center = center;
    }


    public BoundingBox GetBoundingBox()
    {
        return new BoundingBox(box[0], box[1], box[2], box[3]);
    }
}
