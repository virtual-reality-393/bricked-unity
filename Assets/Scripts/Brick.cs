using NUnit.Framework;
using System;
using UnityEngine;

public class Brick
{
    public string colorName;
    public Color BrickColor => GameUtils.nameToColor[colorName];
    public Vector3 worldPos;
    
    public Brick(string colorName,Vector3 worldPos)
    {
        this.colorName = colorName;
        this.worldPos = worldPos;
    }

    public Brick()
    {
        this.colorName = "";
        this.worldPos = Vector3.zero;
    }

    public GameObject Draw()
    {
        var cube = UnityEngine.Object.Instantiate(GameManager.Instance.brickPrefab , worldPos, Quaternion.identity);
        cube.GetComponent<Renderer>().material.color = BrickColor;
        return cube;
    }

    public GameObject Draw(Color color)
    {
        var cube = UnityEngine.Object.Instantiate(GameManager.Instance.brickPrefab, worldPos, Quaternion.identity);
        cube.GetComponent<Renderer>().material.color = color;
        return cube;
    }

    public GameObject DrawSmall()
    {
        var cube = UnityEngine.Object.Instantiate(GameManager.Instance.brickPrefab, worldPos, Quaternion.identity);
        cube.GetComponent<Renderer>().material.color = BrickColor;
        cube.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        return cube;
    }

}


public class DebugBrick : Brick
{
    public Color brickColor;

    DebugBrick()
    {
        this.colorName = "";
        this.worldPos = Vector3.zero;
        this.brickColor = Color.white;
    }
    public DebugBrick(string colorName, Vector3 worldPos)
    {
        this.colorName = colorName;
        this.worldPos = worldPos;
        this.brickColor = GameUtils.nameToColor[colorName];
    }
    public DebugBrick(string colorName, Vector3 worldPos, Color color)
    {
        this.colorName = colorName;
        this.worldPos = worldPos;
        this.brickColor = color;
    }


    public GameObject[] DebugDraw()
    {
        GameObject[] res = new GameObject[2];

        var cube = UnityEngine.Object.Instantiate(GameManager.Instance.brickPrefab, worldPos, Quaternion.identity);
        cube.GetComponent<Renderer>().material.color = GameUtils.nameToColor[colorName];
        res[0] = cube;

        var cube2 = UnityEngine.Object.Instantiate(GameManager.Instance.brickPrefab, worldPos+new Vector3(0,0.03f,0), Quaternion.identity);
        cube2.GetComponent<Renderer>().material.color = brickColor;
        res[1] = cube2;

        return res;
    }
}


[Serializable]
public struct PythonBrick
{
    public string color;
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
    
    public override string ToString()
    {
        return $"PythonBrick [Color: {color}, Box: ({string.Join(", ", box)}), Center: ({string.Join(", ", center)})]";
    }

    
}
