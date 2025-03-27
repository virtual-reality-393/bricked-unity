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

    Brick()
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
