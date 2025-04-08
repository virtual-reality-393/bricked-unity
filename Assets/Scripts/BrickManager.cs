using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BrickManager : MonoBehaviour
{

    public ObjectDetector detector;
    public GameObject canvas;
    public Transform centerCam;
    public float range;
    
    public GameObject brickPrefab;
    public GameObject brickRadiusPrefab;
    
    public List<KVPair<GameObject,int>> bricks = new List<KVPair<GameObject,int>>();
    void Awake()
    {
        detector.OnBricksDetected += OnBricksDetected;

        StartCoroutine(DecreaseLifetime());
    }

    IEnumerator DecreaseLifetime()
    {
        while (true)
        {
            for (int i = bricks.Count - 1; i >= 0; i--)
            {
                if (--bricks[i].Value <= 0)
                {
                    Destroy(bricks[i].Key);
                    bricks.RemoveAt(i);
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void OnBricksDetected(object sender, ObjectDetectedEventArgs e)
    {
        var detectedBricks = e.Bricks;

        var bricksToAdd = new List<KVPair<GameObject,int>>();
        foreach (var detectedBrick in detectedBricks)
        {
            var relevantBricks = bricks.Where((brick) =>
                Vector3.Distance(detectedBrick.worldPos, brick.Key.transform.position) < range);
            
            var kvPairs = relevantBricks.ToList();
            if (kvPairs.Count> 0)
            {
                foreach (var brick in  kvPairs)
                {
                    brick.Value = 5;
                }
            }
            else
            {
                GameObject newBrick = GameObject.Instantiate(brickPrefab);
                newBrick.transform.position = detectedBrick.worldPos;

                // GameObject brickRadius = GameObject.Instantiate(brickRadiusPrefab);
                //     
                // brickRadius.transform.localScale  = new Vector3( range*2, 0.005f, range*2);
                // brickRadius.transform.position = detectedBrick.worldPos;
                //     
                // brickRadius.transform.parent = newBrick.transform;    

                newBrick.name = detectedBrick.labelName;
                    
                newBrick.GetComponent<Renderer>().material.color = Color.cyan;
                    
                bricksToAdd.Add(new KVPair<GameObject, int>(newBrick,5));
            }
        }

        foreach (var brick in bricksToAdd)
        {
            bricks.Add(brick);
        }
        
        foreach (Transform t in canvas.transform)
        {
            Destroy(t.gameObject);
        }

        foreach (var brick in bricks)
        {
            AddText(brick.Value.ToString(), brick.Key.transform.position,Color.magenta);
        }

    }

    public void AddText(string text, Vector3 position, Color color)
    {
        // Create a new GameObject for the text and set its attributes.
        GameObject newGameObject = new GameObject();
        RectTransform rect = newGameObject.AddComponent<RectTransform>();
        rect.position = position + new Vector3(0,0.03f, 0);
        rect.rotation = Quaternion.identity;
        rect.LookAt(centerCam);
        rect.Rotate(Vector3.up, 180);
        rect.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        newGameObject.transform.SetParent(canvas.transform);
        TextMeshPro newText = newGameObject.AddComponent<TextMeshPro>();

        // Set specific TextMeshPro settings, extend this as you see fit.
        newText.text = text;
        newText.fontSize = 1;
        newText.alignment = TextAlignmentOptions.Center;
        newText.color = color;
    }

    public List<GameObject> GetBrickObjects()
    {
        return bricks.Select(brick => brick.Key).ToList();
    }

}

public class KVPair<K, V>
{
    public K Key;
    public V Value;


    public KVPair(K key, V value)
    {
        Key = key;
        Value = value;
    }
}