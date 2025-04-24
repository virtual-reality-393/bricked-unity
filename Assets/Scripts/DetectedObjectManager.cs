using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Meta.XR.MRUtilityKit;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class DetectedObjectManager : MonoBehaviour
{

    [Header("Detection Settings")]
    public ObjectDetector detector;
    public float distanceThreshold = 0.5f;
    public Dictionary<string,List<LifeTimeObject>> LifeTimeObjectsCandidates = new();
    public Dictionary<string,List<LifeTimeObject>> LifeTimeObjects = new();
    public List<InstanceInfo> instanceInfo = new();
    private Dictionary<string, InstanceInfo> _instanceInfo = new();
    private Dictionary<string, List<GameObject>> objectInstances;
    public int minCandidateLifetime;
    public int maxCandidateLifetime;

    public Transform centerCam;
    
    public OVRSpatialAnchor anchor;
    private GameObject anchorObject;


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

    IEnumerator Start()
    {
        detector.OnObjectsDetected += OnObjectDetected;
        // anchorObject = new GameObject("DetectedObjectManager");
        // anchor = anchorObject.AddComponent<OVRSpatialAnchor>();
        // Debug.LogError("Creating anchor");
        // yield return new WaitUntil(() => anchor.Created);
        // Debug.LogError("Created anchor");
        objectInstances = new();
        
        foreach (var maxInstance in instanceInfo)
        {
            objectInstances.Add(maxInstance.label, new List<GameObject>());
            _instanceInfo.Add(maxInstance.label, maxInstance);
        }
        foreach (var labelName in DetectedLabelIdxToLabelName.Values)
        {
            LifeTimeObjects.Add(labelName,new List<LifeTimeObject>());
            LifeTimeObjectsCandidates.Add(labelName,new List<LifeTimeObject>());
        }

        yield return null;
    }

    public void OnObjectDetected(object sender, ObjectDetectedEventArgs args)
    {
        var detectedObjects = args.DetectedObjects;
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

                var orderedLifetime = LifeTimeObjects[lifeTimeObject.labelName].OrderBy(x => x.lifeTime).ToArray();
                var checks = 0;
                for (var i = 0; i < orderedLifetime.Length; i++)
                {
                    var l = orderedLifetime[i];
                    if (Vector3.Distance(l.obj.transform.position, lifeTimeObject.obj.transform.position) >= distanceThreshold)
                    {
                        if (checks++ < _instanceInfo[lifeTimeObject.labelName].maxInstances)
                        {
                            l.lifeTime--;
                        }
                    }
                    else
                    {
                        shouldSpawnObject = false;
                        l.lifeTime = _instanceInfo[l.labelName].defaultLifetime;
                    }
                }

                if (shouldSpawnObject && !(LifeTimeObjects[lifeTimeObject.labelName].Count >= _instanceInfo[lifeTimeObject.labelName].maxInstances))
                {
                    SpawnLifetimeObject(lifeTimeObject);
                }
            }
            
        }
        
        UpdateLifetime();
    }

    private GameObject SpawnLifetimeObject(LifeTimeObject v)
    {
        
        if (objectInstances[v.labelName].Count >= _instanceInfo[v.labelName].maxInstances)
        {
            var closestObj = objectInstances[v.labelName].GetClosest(v.obj.transform.position, x => !x.activeSelf);
            if (closestObj != null)
            {
                LifeTimeObjects[v.labelName].Add(new LifeTimeObject(v.labelIdx,_instanceInfo[v.labelName].defaultLifetime,closestObj,v.labelName));
                closestObj.transform.position = v.obj.transform.position;
                closestObj.SetActive(true);
            }
            return closestObj;
        }

        GameObject go = new GameObject(v.labelName);
        
        go.transform.position = v.obj.transform.position;

        if (_instanceInfo[v.labelName].prefab != null)
        {
            Instantiate(_instanceInfo[v.labelName].prefab, go.transform);
        }
        
        LifeTimeObjects[v.labelName].Add(new LifeTimeObject(v.labelIdx,_instanceInfo[v.labelName].defaultLifetime,go,v.labelName));
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
public class InstanceInfo
{
    public string label;
    public int defaultLifetime;
    public int maxInstances;
    public GameObject prefab;
}

