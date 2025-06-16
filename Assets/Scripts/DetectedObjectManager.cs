using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public class DetectedObjectManager : MonoBehaviour
{

    [Header("Detection Settings")]
    public ObjectDetector detector;
    public float distanceThreshold = 0.5f;
    public List<InstanceInfo> instanceInfo = new();
    public Dictionary<string,List<LifeTimeObject>> LifeTimeObjects = new();
    public int minCandidateLifetime;
    public int maxCandidateLifetime;
    
    private Dictionary<string,List<LifeTimeObject>> _lifeTimeObjectsCandidates = new();
    private Dictionary<string, InstanceInfo> _instanceInfo = new();
    private Dictionary<string, List<GameObject>> _objectInstances;


    private static readonly ReadOnlyDictionary<int, string> DetectedLabelIdxToLabelName = new(new Dictionary<int, string>
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

    private IEnumerator Start()
    {
        detector.OnObjectsDetected += OnObjectDetected;
        _objectInstances = new Dictionary<string, List<GameObject>>();
        
        foreach (var maxInstance in instanceInfo)
        {
            _objectInstances.Add(maxInstance.label, new List<GameObject>());
            _instanceInfo.Add(maxInstance.label, maxInstance);
        }
        foreach (var labelName in DetectedLabelIdxToLabelName.Values)
        {
            LifeTimeObjects.Add(labelName,new List<LifeTimeObject>());
            _lifeTimeObjectsCandidates.Add(labelName,new List<LifeTimeObject>());
        }

        yield return null;
    }

    private void OnObjectDetected(object sender, ObjectDetectedEventArgs args)
    {
        var detectedObjects = args.DetectedObjects;
        foreach (var detectedObject in detectedObjects)
        {
            bool shouldSpawnObject = true;
            if (!_objectInstances.ContainsKey(detectedObject.labelName)) continue;
            for (var i = 0; i < _lifeTimeObjectsCandidates[detectedObject.labelName].Count; i++)
            {
                bool shouldIncreaseLifetime = false;
                
                var l = _lifeTimeObjectsCandidates[detectedObject.labelName][i];
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
                _lifeTimeObjectsCandidates[detectedObject.labelName].Add(new LifeTimeObject(detectedObject.labelIdx,2,candiateObj,detectedObject.labelName));
            }
        }

        foreach (var l in _lifeTimeObjectsCandidates.Values)
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

        foreach (var candidateList in _lifeTimeObjectsCandidates.Values)
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

    private void SpawnLifetimeObject(LifeTimeObject v)
    {
        if (_objectInstances[v.labelName].Count >= _instanceInfo[v.labelName].maxInstances)
        {
            var closestObj = _objectInstances[v.labelName].GetClosest(v.obj.transform.position, x => !x.activeSelf || x.name.Contains("KEEP"));
            if (closestObj != null)
            {
                LifeTimeObjects[v.labelName].Add(new LifeTimeObject(v.labelIdx,_instanceInfo[v.labelName].defaultLifetime,closestObj,v.labelName));
                closestObj.transform.position = v.obj.transform.position;
                closestObj.SetActive(true);
            }
            return;
        }

        GameObject go = new GameObject(v.labelName);
        
        if (_instanceInfo[v.labelName].keepShown)
        {
            go.name = "KEEP";
        }
        
        go.transform.position = v.obj.transform.position;

        if (_instanceInfo[v.labelName].prefab != null)
        {
            Instantiate(_instanceInfo[v.labelName].prefab, go.transform);
        }
        
        LifeTimeObjects[v.labelName].Add(new LifeTimeObject(v.labelIdx,_instanceInfo[v.labelName].defaultLifetime,go,v.labelName));
        _objectInstances[v.labelName].Add(go);
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
                    _objectInstances[l[i].labelName][0].SetActive(_instanceInfo[l[i].labelName].keepShown);
                    l.RemoveAt(i);
                }
                else
                {
                    _objectInstances[l[i].labelName][0].SetActive(true);
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
    public bool keepShown;
}

