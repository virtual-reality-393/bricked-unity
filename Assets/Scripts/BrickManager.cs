using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TMPro;
using UnityEngine;

public class BrickManager : MonoBehaviour
{

    [Header("Detection Settings")]
    public ObjectDetector detector;
    public float distanceThreshold = 0.5f;
    public int detectionLifetime;
    public Dictionary<string,List<LifeTimeObject>> LifeTimeObjects = new();
    public List<KVPair<string, int>> maxInstances = new();
    private Dictionary<string, List<GameObject>> objectInstances;
    
    public static readonly ReadOnlyDictionary<int, string> DetectedLabelIdxToLabelName = new(new Dictionary<int, string>
    {
        {0,"red"},
        {1,"green"},
        {2,"blue"},
        {3,"yellow"},
        {4,"big penguin"},
        {5,"small penguin"},
        {6,"lion"},
        {7,"sheep"},
        {8,"pig"},
        {9,"human"},
    });

    void Start()
    {

        detector.OnObjectsDetected += OnObjectDetected;
        objectInstances = new();
        foreach (var maxInstance in maxInstances)
        {
            objectInstances.Add(maxInstance.Key, new List<GameObject>());
        }
        foreach (var labelName in DetectedLabelIdxToLabelName.Values)
        {
            LifeTimeObjects.Add(labelName,new List<LifeTimeObject>());
        }
    }

    void OnObjectDetected(object sender, ObjectDetectedEventArgs e)
    {
        foreach (var v in e.DetectedObjects)
        {
            if (LifeTimeObjects[v.labelName].Count == 0)
            {
                SpawnLifetimeObject(v);
            }
            else
            {
                for (var i = 0; i < LifeTimeObjects[v.labelName].Count; i++)
                {
                    var l = LifeTimeObjects[v.labelName][i];
                    if (Vector3.Distance(l.obj.transform.position, v.worldPos) >= distanceThreshold)
                    {
                        SpawnLifetimeObject(v);
                    }
                    else
                    {
                        l.lifeTime = detectionLifetime;
                    }
                }
            }
            
        }
        
        UpdateLifetime();
    }

    private GameObject SpawnLifetimeObject(DetectedObject v)
    {
        if (objectInstances[v.labelName].Count > 0)
        {
            objectInstances[v.labelName][0].SetActive(true);
            return objectInstances[v.labelName][0];
        }
        GameObject go = Instantiate(GameManager.Instance.brickPrefab);
        go.GetComponent<Renderer>().material.color = DetectedObject.labelToDrawColor[v.labelIdx];
        go.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        go.transform.position = v.worldPos;
        
        LifeTimeObjects[v.labelName].Add(new LifeTimeObject(detectionLifetime,go,v.labelName));

        objectInstances[v.labelName].Add(go);


        return go;
    }


    private void UpdateLifetime()
    {
        foreach (var l in LifeTimeObjects.Values)
        {
            foreach (var obj in l)
            {
                if(obj.lifeTime <= 0 ) continue;
                obj.lifeTime--;

                var closeObjs = l.Where(x => Vector3.Distance(x.obj.transform.position,obj.obj.transform.position) < distanceThreshold && x != obj);

                foreach (var cObj in closeObjs)
                {
                    cObj.lifeTime = 0;
                }
            }
        }

        foreach (var l in LifeTimeObjects.Values)
        {
            for (int i = l.Count-1;i>=0; i--)
            {
                if (l[i].lifeTime <= 0)
                {
                    objectInstances[l[i].labelName][0].SetActive(false);
                    l.RemoveAt(i);
                }
                else
                {
                    objectInstances[l[i].labelName][0].SetActive(true);
                }
            }
        }
    }
}


public class LifeTimeObject
{
    public float lifeTime;
    public GameObject obj;
    public string labelName;
    public LifeTimeObject(float lifeTime, GameObject obj, string labelName)
    {
        this.lifeTime = lifeTime;
        this.obj = obj;
        this.labelName = labelName;
    }
}

[Serializable]
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

[Serializable]
public class Triplet<K1,K2,K3>
{
    public K1 val1;
    public K2 val2;
    public K3 val3;

    public Triplet(K1 val1, K2 val2, K3 val3)
    {
        this.val1 = val1;
        this.val2 = val2;
        this.val3 = val3;
    }
}
