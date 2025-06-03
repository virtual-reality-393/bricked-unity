using System;
using System.Collections.Generic;
using UnityEngine;



public class DetectedObject
{
    public int labelIdx;
    public string labelName;
    public Vector3 worldPos;
    public Vector2Int screenPos;

    public static Dictionary<int, Color> labelToDrawColor = new Dictionary<int, Color>
    {
        {-1, Color.black },
        {0, new Color(1f, 0f, 0f)},
        {1, new Color(0f, 1f, 0f)},
        {2, new Color(0f, 0f, 1f)},
        {3, new Color(1f, 1f, 0f)},
        {4, new Color(1f, 0.4f, 0.4f)},
        {5, new Color(0.4f, 1f, 0.4f)},
        {6, new Color(0.4f, 1f, 1f)},
        {7, new Color(1f, 1f, 1f)},
        {8, new Color(0.5f, 0.5f, 1f)},
        {9, new Color(0.8f, 0.2f, 1f)},
    };

    public DetectedObject()
    {
        labelIdx = -1;
        labelName = "";
        worldPos = Vector3.zero;
        screenPos = Vector2Int.zero;
    }

    public DetectedObject(int labelIdx, string labelName, Vector3 worldPos)
    {
        this.labelIdx = labelIdx;
        this.labelName = labelName;
        this.worldPos = worldPos;
        screenPos = Vector2Int.zero;
    }
    public DetectedObject(int labelIdx, string labelName, Vector3 worldPos, Vector2Int screenPos)
    {
        this.labelIdx = labelIdx;
        this.labelName = labelName;
        this.worldPos = worldPos;
        this.screenPos = screenPos;
    }

    public GameObject Draw()
    {
        var cube = UnityEngine.Object.Instantiate(GameManager.Instance.cubePrefab , worldPos, Quaternion.identity);
        cube.GetComponent<Renderer>().material.color = labelToDrawColor[labelIdx];
        return cube;
    }

    public GameObject Draw(Color color)
    {
        var cube = UnityEngine.Object.Instantiate(GameManager.Instance.cubePrefab, worldPos, Quaternion.identity);
        cube.GetComponent<Renderer>().material.color = color;
        return cube;
    }

    public GameObject DrawSmall()
    {
        var cube = UnityEngine.Object.Instantiate(GameManager.Instance.cubePrefab, worldPos, Quaternion.identity);
        cube.GetComponent<Renderer>().material.color = labelToDrawColor[labelIdx];
        cube.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        return cube;
    }
    
    public override string ToString()
    {
        return $"DetectedObject [LabelIdx: {labelIdx}, LabelName: {labelName}, WorldPos: {worldPos}, ScreenPos: {screenPos}]";
    }


}

public class DetectedStack
{
    public int x1;
    public int x2;
    public int y1;
    public int y2;


    private Rect boundingBox;


    public DetectedStack(int x1, int x2, int y1, int y2)
    {
        this.x1 = x1;
        this.x2 = x2;
        this.y1 = y1;
        this.y2 = y2;
        
        
        boundingBox = new Rect(x1, y1, x2 - x1, y2 - y1);
    }

    public bool Contains(DetectedObject detectedObject)
    {
        return boundingBox.Contains(detectedObject.screenPos);
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


    public DetectionBox GetBoundingBox()
    {
        return new DetectionBox(0,0,box[0], box[1], box[2], box[3]);
    }
    
    public override string ToString()
    {
        return $"PythonBrick [Color: {color}, Box: ({string.Join(", ", box)}), Center: ({string.Join(", ", center)})]";
    }

    
}
