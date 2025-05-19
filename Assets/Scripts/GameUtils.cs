using Meta.XR.MRUtilityKit;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public static class GameUtils
{
    public static Dictionary<string, Color> nameToColor = new Dictionary<string, Color>()
    {
        { "red", new Color(1f, 0f, 0f) },
        { "green", new Color(0f, 1f, 0f) },
        { "blue", new Color(0f, 0f, 1f) },
        { "yellow", new Color(1f, 1f, 0f) },
        { "big penguin", new Color(1f, 0.4f, 0.4f) },
        { "small penguin", new Color(0.4f, 1f, 0.4f) },
        { "lion", new Color(0.4f, 1f, 1f) },
        { "sheep", new Color(1f, 1f, 1f) },
        { "pig", new Color(0.5f, 0.5f, 1f) },
        { "human", new Color(0.8f, 0.2f, 1f) },

    };

    ///Old color code
    //public static string GetColorName(int h, int s, int v)
    //{
    //    h = h * 2;
    //    s = s * 2;
    //    v = v * 2;

    //    // Uncomment and adjust if needed for other conditions
    //    // if (v < 50)
    //    // {
    //    //     return "black";
    //    // }
    //    // else if (v > 200 && s < 100)
    //    // {
    //    //     return "white";
    //    // }

    //    if (h < 30 || h > 330)
    //    {
    //        return "red";
    //    }
    //    else if (h >= 30 && h < 90)
    //    {
    //        return "yellow";
    //    }
    //    else if (h >= 90 && h < 150)
    //    {
    //        return "green";
    //    }
    //    // Uncomment and adjust if needed for other conditions
    //    // else if (h >= 150 && h < 210)
    //    // {
    //    //     return "cyan";
    //    // }
    //    else if (h >= 210 && h < 270)
    //    {
    //        return "blue";
    //    }
    //    // Uncomment and adjust if needed for other conditions
    //    // else if (h >= 270 && h < 330)
    //    // {
    //    //     return "magenta";
    //    // }

    //    return "magenta"; // Default to magenta if none of the conditions matchedW
    //}

    //public static Color GetColorByName(string colorName)
    //{
    //    return nameToColor[colorName];
    //}

    //public static (double H, double S, double V) RgbToHsv(int r, int g, int b)
    //{
    //    double h = 0, s = 0, v = 0;

    //    double rNormalized = r / 255.0;
    //    double gNormalized = g / 255.0;
    //    double bNormalized = b / 255.0;

    //    double max = Math.Max(rNormalized, Math.Max(gNormalized, bNormalized));
    //    double min = Math.Min(rNormalized, Math.Min(gNormalized, bNormalized));
    //    double delta = max - min;

    //    v = max; // Value is the max component

    //    if (delta > 0)
    //    {
    //        s = delta / max; // Saturation is the difference divided by the max value

    //        if (rNormalized == max)
    //        {
    //            h = (gNormalized - bNormalized) / delta; // Red is the max
    //        }
    //        else if (gNormalized == max)
    //        {
    //            h = 2.0 + (bNormalized - rNormalized) / delta; // Green is the max
    //        }
    //        else
    //        {
    //            h = 4.0 + (rNormalized - gNormalized) / delta; // Blue is the max
    //        }

    //        h *= 60; // Convert to degrees

    //        if (h < 0)
    //        {
    //            h += 360; // Make sure hue is positive
    //        }
    //    }

    //    return (h, s * 100, v * 100); // Return H, S, and V (S and V in percentage)
    //}

    ////public static (double H, double S, double V) RgbToHsv(Color color)
    ////{
    ////    return RgbToHsv((int)(color.r * 255), (int)(color.g * 255), (int)(color.b * 255));
    ////}

    //public static string GetAverageColorName(Texture2D texture, DetectionBox box, Vector2Int screenpoint)
    //{
    //    int x = screenpoint.x;
    //    int y = screenpoint.y;

    //    float scale = 0.1f;

    //    //var hvsTex = ConvertRGBToHSV(texture);

    //    Color[] pixels = texture.GetPixels(x, y, (int)(box.Width * scale), (int)(box.Height * scale));

    //    double h = 0, s = 0, v = 0;

    //    // Loop through each pixel to calculate the average HSV
    //    int totalPixels = pixels.Length;
    //    foreach (var pixel in pixels)
    //    {
    //        // Assuming ConvertRGBToHSV returns a Color object with H, S, and V properties
    //        var hsv = RGBToHSV(pixel); // Convert the color pixel to HSV
    //        h += hsv.H;
    //        s += hsv.S;
    //        v += hsv.V;
    //    }

    //    // Calculate average values for H, S, V
    //    h /= totalPixels;
    //    s /= totalPixels;
    //    v /= totalPixels;


    //    return GetColorName((int)h, (int)s, (int)v);
    //}

    //public static (float H, float S, float V) RGBToHSV(Color color)
    //{
    //    float h, s, v;
    //    Color.RGBToHSV(color, out h, out s, out v);
    //    return (h, s, v);
    //}

    //public static string GetClosestColorName(Color color)
    //{
    //    // Get the closest color name based on the given color
    //    string closestColorName = "unknown";
    //    float closestDistance = float.MaxValue;
    //    foreach (var kvp in nameToColor)
    //    {
    //        float distance = GetColorDistance(color, kvp.Value);
    //        if (distance < closestDistance)
    //        {
    //            closestDistance = distance;
    //            closestColorName = kvp.Key;
    //        }
    //    }
    //    return closestColorName;
    //}

    //private static float GetColorDistance(Color color1, Color color2)
    //{
    //    return Mathf.Sqrt(Mathf.Pow(color1.r - color2.r, 2) + Mathf.Pow(color1.g - color2.g, 2) + Mathf.Pow(color1.b - color2.b, 2));
    //}

    //public static Texture2D ConvertRGBToHSV(Texture2D inputTexture)
    //{
    //    // Get pixel data from the input texture
    //    Color[] pixels = inputTexture.GetPixels();

    //    // Create an array to store the converted HSV values (will store them as Color objects)
    //    Color[] hsvPixels = new Color[pixels.Length];

    //    // Loop through each pixel and convert RGB to HSV
    //    for (int i = 0; i < pixels.Length; i++)
    //    {
    //        Color rgb = pixels[i];

    //        // Convert RGB to HSV (Color class provides RGB -> HSV conversion)
    //        float h, s, v;
    //        Color.RGBToHSV(rgb, out h, out s, out v);

    //        // Store the HSV value back in the new array as a Color
    //        hsvPixels[i] = new Color(h, s, v);
    //    }

    //    // Create a new Texture2D with the same dimensions as the input
    //    Texture2D hsvTexture = new Texture2D(inputTexture.width, inputTexture.height);

    //    return hsvTexture;

    //}

    public static List<GameObject> DrawStack(List<string> stack, Vector3 pos)
    {
        List<GameObject> res = new List<GameObject>();
        for (int i = 0; i < stack.Count; i++)
        {
            GameObject cube = GameObject.Instantiate(GameManager.Instance.brickPrefab, pos + new Vector3(0, 0.03f, 0) * i, Quaternion.identity);
            var drawColor = GameUtils.nameToColor[stack[i]];
            drawColor.a =0.8f;
            cube.GetComponent<Renderer>().material.color = drawColor;
            res.Add(cube);
        }
        return res;
    }

    public static List<string> GenetateStack(List<string> sortedList)
    {
        System.Random rng = new System.Random();
        List<string> shuffled = sortedList.OrderBy(x => rng.Next()).ToList();

        List<string> res = new List<string>();
        for (int i = 0; i < shuffled.Count; i++)
        {
            if (i == 0)
            {
                res.Add(shuffled[i]);
            }
            else if (shuffled[i] == shuffled[i - 1])
            {
                continue;
            }
            else
            {
                res.Add(shuffled[i]);
            }
        }
        return res;
    }

    public static List<string> GenetateStack(Dictionary<string, int> input)
    {
        List<string> sortedList = GenerateListFromDict(input);
        return GenetateStack(sortedList);
    }

    public static List<string> GenerateListFromDict(Dictionary<string, int> input)
    {
        List<string> result = new List<string>();

        // Loop through each key-value pair in the dictionary
        foreach (var pair in input)
        {
            // Add the key to the result list "value" number of times
            for (int i = 0; i < pair.Value; i++)
            {
                result.Add(pair.Key);
            }
        }

        return result;
    }

    public static List<List<string>> SplitStackRandomly(List<string> masterStack, int maxSize)
    {
        
        var result = new List<List<string>>();

        if (masterStack == null || maxSize <= 0)
            return result;

        int index = 0;
        while (index < masterStack.Count)
        {
            // Get a random size between 1 and maxSize, but not more than the remaining items
            int remaining = masterStack.Count - index;
            int chunkSize = Random.Range(1, Math.Min(maxSize, remaining) + 1);

            var chunk = masterStack.GetRange(index, chunkSize);
            result.Add(chunk);

            index += chunkSize;
        }

        return result;
    }

    public static Transform[] DiskSampledSpawnPoints(MRUKAnchor tableAnchor,int numberOfPoints, Transform parent)
    {
        Transform[] points = new Transform[numberOfPoints];
        if (tableAnchor.PlaneRect.HasValue)
        {
            var tablePlane = tableAnchor.PlaneRect.Value;

            List<Vector2> allPoints = DiskSampling.GenerateDiskSamples(tablePlane, 5, 50, 10);

            for (int i = 0; i < numberOfPoints; i++)
            {
                var point = allPoints[Random.Range(0, allPoints.Count)];

                var newObject = new GameObject();

                newObject.transform.parent = tableAnchor.transform;

                newObject.transform.position = tableAnchor.transform.position;

                newObject.transform.localPosition = point;

                newObject.transform.parent = parent;

                points[i] = newObject.transform;
                allPoints.Remove(point);
            }

        }
        else
        {
            points = null;
        }

        return points;
    }

    public static Transform[] DiskSampledSpawnPoints(MRUKAnchor tableAnchor, int numberOfPoints, Transform parent, Rect costumRect)
    {
        Transform[] points = new Transform[numberOfPoints];
        var tablePlane = costumRect;

        List<Vector2> allPoints = DiskSampling.GenerateDiskSamples(tablePlane, 5, 50, 10);

        for (int i = 0; i < numberOfPoints; i++)
        {
            var point = allPoints[Random.Range(0, allPoints.Count)];

            var newObject = new GameObject();

            newObject.transform.parent = tableAnchor.transform;

            newObject.transform.position = tableAnchor.transform.position;

            newObject.transform.localPosition = point;

            newObject.transform.parent = parent;

            points[i] = newObject.transform;
            allPoints.Remove(point);
        }

        return points;
    }


    public static DetectedObject GetBrickWithColor(List<DetectedObject> bricks, string color)
    {
        foreach (var brick in bricks)
        {
            if (brick.labelName == color)
            {
                return brick;
            }
        }
        return null;
    }

    public static LifeTimeObject GetLifeTimeObjectWithlabel(List<LifeTimeObject> lifeTimeObjects, string label)
    {
        foreach (var lto in lifeTimeObjects)
        {
            if (lto.labelName == label)
            {
                return lto;
            }
        }
        return null;
    }

    public static float[,] PointsStackDistansMat(List<List<DetectedObject>> stacks, Transform[] points)
    {
        int size1 = points.Length;
        int size2 = stacks.Count;

        // Create a distance matrix with dimensions (size1 x size2)
        float[,] distanceMatrix = new float[size1, size2];

        for (int i = 0; i < size1; i++)
        {
            for (int j = 0; j < size2; j++)
            {
                distanceMatrix[i, j] = Vector3.Distance(points[i].position, stacks[j][0].worldPos);
            }
        }

        return distanceMatrix;
    }


    public static GameObject MakeInteractionCirkle(Vector3 pos, Color color)
    {
        GameObject circle = GameObject.Instantiate(GameManager.Instance.cylinderPrefab, pos, Quaternion.identity);
        color.a = 0.4f;
        circle.GetComponent<Renderer>().material.color = color;
        circle.transform.localScale = new Vector3(0.05f, 0.005f, 0.05f);
        return circle;
    }

    // Method to add a text to the UI with specified attributes.
    public static void AddText(Transform centerCam, GameObject canvas, string text, Vector3 position, Color color, float fontsize = 1)
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

    // Method to add a text to the UI with specified attributes.
    public static GameObject AddText(string text, Vector3 position, Color color, float fontsize = 1)
    {
        // Create a new GameObject for the text and set its attributes.
        GameObject newGameObject = new GameObject();
        RectTransform rect = newGameObject.AddComponent<RectTransform>();
        rect.position = position + new Vector3(0, 0.03f, 0);
        rect.rotation = Quaternion.identity;
        rect.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        TextMeshPro newText = newGameObject.AddComponent<TextMeshPro>();

        // Set specific TextMeshPro settings, extend this as you see fit.
        newText.text = text;
        newText.fontSize = fontsize;
        newText.alignment = TextAlignmentOptions.Center;
        newText.color = color;

        return newGameObject;
    }

    public static List<int> ClosestStacks(float[,] distMat)
    {
        List<int> res = new List<int>();
        for (int i = 0; i < distMat.GetLength(0); i++)
        {
            int idx = 0;
            float min = 1000;
            for (int j = 0; j < distMat.GetLength(1); j++)
            {
                if (distMat[i, j] < min)
                {
                    min = distMat[i, j];
                    idx = j;
                }
            }
            res.Add(idx);
        }
        return res;
    }

    public static bool HaveSameElementAtSameIndex(List<string> list1, List<string> list2)
    {
        // Check if both lists have the same number of elements
        if (list1.Count != list2.Count)
        {
            return false;
        }

        // Loop through the lists and compare the elements at each index
        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i] != list2[i])
            {
                return false;
            }
        }

        // If all elements match, return true
        return true;
    }

    public static bool HaveSameElements(List<string> list1, List<string> list2)
    {
        if (list1.Count != list2.Count)
        {
            return false;
        }

        var grouped1 = list1.GroupBy(x => x)
                            .ToDictionary(g => g.Key, g => g.Count());

        var grouped2 = list2.GroupBy(x => x)
                            .ToDictionary(g => g.Key, g => g.Count());

        return grouped1.Count == grouped2.Count &&
               grouped1.All(kv => grouped2.TryGetValue(kv.Key, out int count) && count == kv.Value);
    }

    public static List<string> DetectedObjectListToStringList(List<DetectedObject> detectedObjects)
    {
        List<string> res = new List<string>();
        foreach (var obj in detectedObjects)
        {
            res.Add(obj.labelName);
        }
        return res;
    }

    public static void GetTwoLowestIndices(float[] arr, out int minIndex1, out int minIndex2)
    {
        minIndex1 = -1;
        minIndex2 = -1;
        if (arr == null || arr.Length < 2)
            return;
        
        minIndex1 = 0;
        minIndex2 = 1;
        
        if (arr[minIndex2] < arr[minIndex1])
        {
            (minIndex1, minIndex2) = (minIndex2, minIndex1);
        }

        for (int i = 2; i < arr.Length; i++)
        {
            if (arr[i] < arr[minIndex1])
            {
                minIndex2 = minIndex1;
                minIndex1 = i;
            }
            else if (arr[i] < arr[minIndex2])
            {
                minIndex2 = i;
            }
        }
    }

    public static int[,] ClosestBricks(float[,] distArr, float threshold)
    {
        int[,] ids = new int[distArr.GetLength(0), 2];
        for (int i = 0; i < distArr.GetLength(0); i++)
        {
            float[] row = new float[distArr.GetLength(1)];
            for (int j = 0; j < distArr.GetLength(1); j++)
            {
                row[j] = distArr[i, j];
            }

            GetTwoLowestIndices(row, out var minIndex1, out var minIndex2);
            
            if(minIndex1 == -1 || minIndex2 == -1)
            {
                ids[i, 0] = -1;
                ids[i, 1] = -1;
                continue;
            }
            ids[i, 0] = distArr[i, minIndex1] < threshold ? minIndex1 : -1;
            ids[i, 1] = distArr[i, minIndex2] < threshold ? minIndex2 : -1;
        }
        return ids;
    }

    public static float[,] DistMat(List<DetectedObject> bricks)
    {
        float[,] distArr = new float[bricks.Count, bricks.Count];
        for (int i = 0; i < bricks.Count; i++)
        {
            for (int j = 0; j < bricks.Count; j++)
            {
                if (i == j)
                {
                    distArr[i, j] = 10000;
                }
                else
                {
                    distArr[i, j] = Vector3.Distance(bricks[i].worldPos, bricks[j].worldPos);
                }
            }
        }
        return distArr;
    }


    public static List<List<int>> FindConnectedComponents(int[,] graph)
    {
        int n = graph.GetLength(0);
        List<List<int>> components = new List<List<int>>();
        HashSet<int> visited = new HashSet<int>();

        for (int i = 0; i < n; i++)
        {
            if (!visited.Contains(i))
            {
                List<int> component = new List<int>();
                DFS(graph, i, visited, component);
                components.Add(component);
            }
        }

        return components;
    }

    public static void DFS(int[,] graph, int node, HashSet<int> visited, List<int> component)
    {
        Stack<int> stack = new Stack<int>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            int curr = stack.Pop();
            if (!visited.Contains(curr))
            {
                visited.Add(curr);
                component.Add(curr);

                // Add connected nodes to stack
                for (int j = 0; j < 2; j++)
                {
                    int neighbor = graph[curr, j];
                    if (neighbor != -1 && !visited.Contains(neighbor))
                    {
                        stack.Push(neighbor);
                    }
                }
            }
        }
    }
}
