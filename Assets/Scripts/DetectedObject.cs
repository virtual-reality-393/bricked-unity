using System;
using System.Collections.Generic;
using UnityEngine;



public class DetectedObject
{
    public int labelIdx;
    public string labelName;
    public Vector3 worldPos;

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
    }

    public DetectedObject(int labelIdx, string labelName, Vector3 worldPos)
    {
        this.labelIdx = labelIdx;
        this.labelName = labelName;
        this.worldPos = worldPos;
    }

    public GameObject Draw()
    {
        var cube = UnityEngine.Object.Instantiate(GameManager.Instance.brickPrefab , worldPos, Quaternion.identity);
        cube.GetComponent<Renderer>().material.color = labelToDrawColor[labelIdx];
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
        cube.GetComponent<Renderer>().material.color = labelToDrawColor[labelIdx];
        cube.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        return cube;
    }
    
    public override string ToString()
    {
        return $"DetectedObject [LabelIdx: {labelIdx}, LabelName: {labelName}, WorldPos: {worldPos}]";
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
