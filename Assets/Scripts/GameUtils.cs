using Meta.XR.MRUtilityKit;
using System;
using System.Collections;
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

    public static List<GameObject> DrawStack(List<string> stack, Vector3 pos)
    {
        List<GameObject> res = new List<GameObject>();
        for (int i = 0; i < stack.Count; i++)
        {
            GameObject cube = GameObject.Instantiate(GameManager.Instance.GetBrick(stack[i]), pos + new Vector3(0, 0.0208f, 0) * i, Quaternion.identity);
            cube.SetActive(false);
            GameManager.Instance.StartCoroutine(
                FallingBrickEffect(
                    cube,
                    pos + new Vector3(0, 0.0208f, 0) * i + Vector3.up * 0.2f,
                    pos + new Vector3(0, 0.0208f, 0) * i,
                    0.6f,
                    -0.25f * i,
                    i < stack.Count - 1,
                   i > 0
                    )
                );
            res.Add(cube);
        }
        return res;
    }

    static IEnumerator FallingBrickEffect(GameObject brick, Vector3 startPos, Vector3 endPos, float timeToComplete,
        float t = 0f, bool disableTop = false, bool disableBottom = false)
    {
        var spawned = false;

        while (t < 1f)
        {
            t += Time.deltaTime/timeToComplete;
            if (t > 0)
            {
                if (!spawned)
                {
                    brick.SetActive(true);
                    spawned = true;
                }
                
                
                brick.transform.position = Vector3.Lerp(startPos, endPos, Mathf.Clamp(t, 0f, 1f));
            }
            yield return null;   
        }

        yield return new WaitForSeconds(0.25f*timeToComplete);
        
        if (disableBottom)
        {
            brick.GetComponent<Brick>().bottom.SetActive(false);
        }
        
        if (disableTop)
        {
            brick.GetComponent<Brick>().top.SetActive(false);
        }
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

    public static Transform[] DiskSampledSpawnPoints(MRUKAnchor tableAnchor, int numberOfPoints, Transform parent, Rect costumRect, Transform headPos, float minDist ,float maxDist,PointSide pointSide = PointSide.Both , bool showPoints = false)
    {
        Transform[] points = new Transform[numberOfPoints];
        var tablePlane = costumRect;

        List<Vector2> allPoints = DiskSampling.GenerateDiskSamples(tablePlane, 8, 50, 10);
        Vector3 localheadPos = tableAnchor.transform.InverseTransformPoint(headPos.position);
        Vector2 pos = new Vector2(localheadPos.x, localheadPos.y);
        List<Vector2> vaildPoints = new List<Vector2>();
        int attempts = 0;
        while (vaildPoints.Count < numberOfPoints && attempts < 1000)
        {
            Debug.Log($"Attempts: {attempts}, Headpos: {headPos.position.ToString()}");
            vaildPoints = new List<Vector2>();
            List<Vector2> pointOnRightSide = new List<Vector2>();
            for (int i = 0; i < allPoints.Count; i++)
            {
                if (pointSide == PointSide.Right)
                {
                    if (allPoints[i].x >= 0)
                    {
                        pointOnRightSide.Add(allPoints[i]);
                    }
                }
                else if (pointSide == PointSide.Left)
                {
                    if (allPoints[i].x <= 0)
                    {
                        pointOnRightSide.Add(allPoints[i]);
                    }
                }
                else
                {
                    pointOnRightSide.Add(allPoints[i]);
                }
            }

            List<Vector2> minDistPoints = new List<Vector2>();
            for (int i = 0; i < pointOnRightSide.Count; i++)
            {
                if (Vector2.Distance(pos, pointOnRightSide[i]) >= minDist)
                {
                    minDistPoints.Add(pointOnRightSide[i]);
                }
            }

            for (int i = 0; i < minDistPoints.Count; i++)
            {
                if (Vector2.Distance(pos, minDistPoints[i]) <= maxDist)
                {
                    vaildPoints.Add(minDistPoints[i]);
                }
            }

            minDist -= 0.01f; // Decrease the minimum distance to find more valid points
            maxDist += 0.01f; // Increase the maximum distance to find more valid points

            attempts++;

            if (attempts >= 1000)
            {
                allPoints = DiskSampling.GenerateDiskSamples(tablePlane, 8, 50, 10);
                localheadPos = tableAnchor.transform.InverseTransformPoint(headPos.position);
                pos = new Vector2(localheadPos.x, localheadPos.y);
                attempts = 0;
            }
        }

        if (showPoints)
        {
            for (int i = 0; i < allPoints.Count; i++)
            {
                Color color = Color.white;
                if (vaildPoints.Contains(allPoints[i]))
                {
                    color = Color.green;
                }
                else
                {
                    color = Color.blue;
                }
                var newObject = MakeInteractionCirkle(allPoints[i], color);
                newObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

                newObject.transform.parent = tableAnchor.transform;

                newObject.transform.position = tableAnchor.transform.position;

                newObject.transform.localPosition = allPoints[i];

                newObject.transform.parent = parent;
            }
        }

        for (int i = 0; i < numberOfPoints; i++)
        {
            var point = vaildPoints[Random.Range(0, vaildPoints.Count)];

            var newObject = new GameObject();

            newObject.transform.parent = tableAnchor.transform;

            newObject.transform.position = tableAnchor.transform.position;

            newObject.transform.localPosition = point;

            newObject.transform.parent = parent;

            points[i] = newObject.transform;
            vaildPoints.Remove(point);
        }

        return points;
    }

    public static Vector3 ClosestPointOnSegment(Vector3 A, Vector3 B, Vector3 P)
    {
        Vector3 AB = B - A;
        float t = Vector3.Dot(P - A, AB) / AB.sqrMagnitude;
        t = Mathf.Clamp01(t); // Clamp to segment
        return A + t * AB;
    }

    // Returns the closest point on the edge of the quadrilateral to point P
    // The 4 corners (A,B,C,D) are ordered clockwise or counter-clockwise.
    public static Vector3 ClosestPointOnQuadEdge(Vector3 A, Vector3 B, Vector3 C, Vector3 D, Vector3 P)
    {
        Vector3[] quadPoints = new Vector3[] { A, B, C, D };
        Vector3 closestPoint = Vector3.zero;
        float minDistance = float.MaxValue;

        for (int i = 0; i < 4; i++)
        {
            Vector3 edgeStart = quadPoints[i];
            Vector3 edgeEnd = quadPoints[(i + 1) % 4];
            Vector3 pointOnEdge = ClosestPointOnSegment(edgeStart, edgeEnd, P);
            float distSqr = (P - pointOnEdge).sqrMagnitude;

            if (distSqr < minDistance)
            {
                minDistance = distSqr;
                closestPoint = pointOnEdge;
            }
        }

        return closestPoint;
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
}
