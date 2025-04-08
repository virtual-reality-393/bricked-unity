using System;
using UnityEngine;

[Serializable]
public struct DetectionBox
{
    public readonly int label;
    public readonly int x1;
    public readonly int y1;
    public readonly int x2;
    public readonly int y2;

    public readonly int Width => x2 - x1;
    public readonly int Height => y2 - y1;

    public DetectionBox(int label, int x1, int y1, int x2, int y2)
    {

        if (x1 >= x2 || y1 > y2)
        {
            throw new ArgumentException("x1 must be smaller than x2 and y1 must be smaller than y2");
        }

        this.label = label;
        this.x1 = x1;
        this.y1 = y1;
        this.x2 = x2;
        this.y2 = y2;
    }

    public Vector2Int GetCenter()
    {
        return new Vector2Int(x1 + (x2 - x1) / 2, y1 + (y2 - y1) / 2);
    }

    public int GetArea()
    {
        return (x2 - x1) * (y2 - y1);
    }

    public DetectionBox Intersect(DetectionBox otherBox)
    {
        int newx1 = Math.Max(this.x1, otherBox.x1);
        int newx2 = Math.Min(this.x2, otherBox.x2);

        int newy1 = Math.Max(this.y1, otherBox.y1);
        int newy2 = Math.Min(this.y2, otherBox.y2);

        if (newx1 > newx2 || newy1 > newy2)
        {
            return new DetectionBox();
        }

        return new DetectionBox(label,newx1, newy1, newx2, newy2);
    }


    public Rect ToRect()
    {
        return new Rect(x1, y1, x2 - x1, y2 - y1);
    }

    public override string ToString()
    {
        return $"({x1},{y1},{x2},{y2})";
    }
}
