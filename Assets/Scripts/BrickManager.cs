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
    public Dictionary<string,List<LifeTimeObject>> LifeTimeObjectsCandidates = new();
    public Dictionary<string,List<LifeTimeObject>> LifeTimeObjects = new();
    public List<KVPair<string, int>> maxInstances = new();
    private Dictionary<string, int> _maxInstance = new();
    private Dictionary<string, List<GameObject>> objectInstances;
    public int minCandidateLifetime;
    public int maxCandidateLifetime;


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
            _maxInstance.Add(maxInstance.Key, maxInstance.Value);
        }
        foreach (var labelName in DetectedLabelIdxToLabelName.Values)
        {
            LifeTimeObjects.Add(labelName,new List<LifeTimeObject>());
            LifeTimeObjectsCandidates.Add(labelName,new List<LifeTimeObject>());
        }
    }

    void DetectionStart()
    {
        objectInstances = new();
        foreach (var maxInstance in maxInstances)
        {
            objectInstances.Add(maxInstance.Key, new List<GameObject>());
            _maxInstance.Add(maxInstance.Key, maxInstance.Value);
        }
        foreach (var labelName in DetectedLabelIdxToLabelName.Values)
        {
            LifeTimeObjects.Add(labelName,new List<LifeTimeObject>());
            LifeTimeObjectsCandidates.Add(labelName,new List<LifeTimeObject>());
        }
    }
    
    void OnObjectDetected(object sender, ObjectDetectedEventArgs objectDetectedEventArgs)
    {
        var detectedObjects = objectDetectedEventArgs.DetectedObjects;
        foreach (var detectedObject in detectedObjects)
        {
            bool shouldSpawnObject = true;
            for (var i = 0; i < LifeTimeObjectsCandidates[detectedObject.labelName].Count; i++)
            {
                bool shouldIncreaseLifetime = false;
                
                var l = LifeTimeObjectsCandidates[detectedObject.labelName][i];
                if (Vector3.Distance(l.obj.transform.position, detectedObject.worldPos) < distanceThreshold)
                {
                    shouldSpawnObject = false;
                    shouldIncreaseLifetime = true;
                }

                if (shouldIncreaseLifetime)
                {
                    l.lifeTime = Math.Min(maxCandidateLifetime+1,l.lifeTime+2);
                }
            }
            
            if (shouldSpawnObject)
            {
                var candiateObj = new GameObject(detectedObject.labelName);
                candiateObj.transform.position = detectedObject.worldPos;
                LifeTimeObjectsCandidates[detectedObject.labelName].Add(new LifeTimeObject(detectedObject.labelIdx,2,candiateObj,detectedObject.labelName));
            }
        }

        foreach (var l in LifeTimeObjectsCandidates.Values)
        {
            for (int i = l.Count-1;i>=0; i--)
            {
                if (l[i].lifeTime <= 0)
                {
                    Destroy(l[i].obj);
                    l.RemoveAt(i);
                }
                else
                {
                    l[i].lifeTime -= 1;
                }
            }
        }
        
        
        
        
        
        foreach (var candidateList in LifeTimeObjectsCandidates.Values)
        {
            foreach (var lifeTimeObject in candidateList)
            {
                if (lifeTimeObject.lifeTime < minCandidateLifetime)
                {
                    continue;
                }

                bool shouldSpawnObject = true;

                var orderedLifetime = LifeTimeObjects[lifeTimeObject.labelName].OrderBy((x) => x.lifeTime).ToArray();
                var checks = 0;
                for (var i = 0; i < orderedLifetime.Length; i++)
                {
                    var l = orderedLifetime[i];
                    if (Vector3.Distance(l.obj.transform.position, lifeTimeObject.obj.transform.position) >= distanceThreshold)
                    {
                        if (checks++ < maxInstances[lifeTimeObject.labelIdx].Value)
                        {
                            l.lifeTime--;
                        }
                    }
                    else
                    {
                        shouldSpawnObject = false;
                        l.lifeTime = detectionLifetime;
                    }
                }

                if (shouldSpawnObject && !(LifeTimeObjects[lifeTimeObject.labelName].Count >= _maxInstance[lifeTimeObject.labelName]))
                {
                    SpawnLifetimeObject(lifeTimeObject);
                }
            }
            
        }
        
        UpdateLifetime();
    }

    private GameObject SpawnLifetimeObject(LifeTimeObject v)
    {
        
        if (objectInstances[v.labelName].Count >= _maxInstance[v.labelName])
        {
            var closestObj = objectInstances[v.labelName].GetClosest(v.obj.transform.position, x => !x.activeSelf);
            if (closestObj != null)
            {
                LifeTimeObjects[v.labelName].Add(new LifeTimeObject(v.labelIdx,detectionLifetime,closestObj,v.labelName));
                closestObj.transform.position = v.obj.transform.position;
                closestObj.SetActive(true);
            }
            return closestObj;
        }
        
        GameObject go = Instantiate(GameManager.Instance.brickPrefab,v.obj.transform.position,GameManager.Instance.brickPrefab.transform.rotation);
        
        go.transform.position = v.obj.transform.position;
        LifeTimeObjects[v.labelName].Add(new LifeTimeObject(v.labelIdx,detectionLifetime,go,v.labelName));
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
    public int labelIdx;
    public int lifeTime;
    public GameObject obj;
    public string labelName;

    public LifeTimeObject()
    {
        lifeTime = 0;
        obj = null;
        labelName = "";
    }

    public LifeTimeObject(int labelIdx, int lifeTime, GameObject obj, string labelName)
    {
        this.labelIdx = labelIdx;
        this.lifeTime = lifeTime;
        this.obj = obj;
        this.labelName = labelName;
    }
    
    public override string ToString()
    {
        return $"LifeTimeObject(LabelIdx: {labelIdx}, LifeTime: {lifeTime}, Obj: {obj?.name ?? "null"}, LabelName: {labelName})";
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
