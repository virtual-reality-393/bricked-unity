using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiskSampling
{
    public int x;
    public int y;
    public int rad;
    public int at;

    public static int width;
    public static int height;
    public static int samples;
    static float cellSize;
    const int DIMS = 2;
    public static List<Vector2> listOfActivePoints = new List<Vector2>();
    static Vector2[,] grid;

    public GameObject pointObject;

    public GameObject cube;
    public static List<List<Vector2>> CleanDiskSampling(Vector2[,] grid)
    {
        List<List<Vector2>> cityPoints = new List<List<Vector2>>();
        for (int j = 0; j < grid.GetLength(0); j++)
        {
            List<Vector2> newList = new List<Vector2>();
            for (int i = 0; i < grid.GetLength(1); i++)
            {
                if (grid[i, j].x != Mathf.NegativeInfinity && grid[i, j].y != Mathf.NegativeInfinity)
                {
                    newList.Add(grid[i, j]);
                }
            }
            if (newList.Count != 0)
            {
                cityPoints.Add(newList);
            }
        }
        return cityPoints;
    }

    static void InsertPoint(Vector2 point)
    {
        grid[Mathf.FloorToInt(point.x / cellSize), Mathf.FloorToInt(point.y / cellSize)] = point;
    }

    public static List<Vector2> GenerateDiskSamples(float radius, int attempts, int w, int h, out Vector2[,] outGrid)
    {
        width = w;
        height = h;
        cellSize = Mathf.FloorToInt(radius / Mathf.Sqrt(DIMS));
        Debug.Log(cellSize);
        grid = new Vector2[Mathf.CeilToInt(width / cellSize) + 1, Mathf.CeilToInt(height / cellSize) + 1];
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                grid[i, j] = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);
            }
        }
        InsertPoint(new Vector2(width / 2, height / 2));
        List<Vector2> points = new List<Vector2>();

        listOfActivePoints.Add(new Vector2(width / 2, height / 2));
        while (listOfActivePoints.Count > 0)
        {
            bool valid = false;
            int randomIndex = Random.Range(0, listOfActivePoints.Count);
            for (int i = 0; i < attempts; i++)
            {
                float angle = Random.Range(0, Mathf.PI * 2);
                float newRadius = Random.Range(radius, radius * 2);
                float pointX = listOfActivePoints[randomIndex].x + newRadius * Mathf.Cos(angle);
                float pointY = listOfActivePoints[randomIndex].y + newRadius * Mathf.Sin(angle);
                Vector2 point = new Vector2(pointX, pointY);

                if (!CheckPoint(point, radius))
                {
                    continue;
                }

                listOfActivePoints.Add(point);
                points.Add(point);
                InsertPoint(point);
                valid = true;
                break;
            }

            if (!valid)
            {
                listOfActivePoints.RemoveAt(randomIndex);
            }
        }
        outGrid = grid;
        return points;
    }

    static bool CheckPoint(Vector2 point, float radius)
    {
        if (point.x < 0  || point.x > width ||  point.y < 0 || point.y > height)
        {
            return false;
        }

        int xIndex = Mathf.FloorToInt(point.x / cellSize);
        int yIndex = Mathf.FloorToInt(point.y / cellSize);
        int minX = Mathf.Max(xIndex - 2, 0);
        int maxX = Mathf.Min(xIndex + 2, width - 1);
        int minY = Mathf.Max(yIndex - 2, 0);
        int maxY = Mathf.Min(yIndex + 2, height - 1);
        for (int i = minX; i < maxX; i++)
        {
            for (int j = minY; j < maxY; j++)
            {
                if (Vector2.Distance(grid[i, j], point) < radius)
                {
                    return false;
                }
            }
        }
        return true;
    }
}