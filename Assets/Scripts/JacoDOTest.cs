using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class JacoDOTest : MonoBehaviour
{
    public ObjectDetector objectDetection;

    public GameObject spawnPositions;
    public GameObject cubeParent;
    public GameObject canvas;

    public Transform centerCam;

    [Header("Detection Settings")]
    public float distanceThreshold = 0.01f;
    public Dictionary<string, List<LifeTimeObject>> LifeTimeObjects = new();

    public Dictionary<string, int> nameToIndex;

    List<GameObject> drawnObjects = new List<GameObject>();

    MRUKRoom room;
    List<MRUKAnchor> anchors = new();
    bool runOnce = true;

    Vector3 anchorPoint = new Vector3(0, 0, 0);
    Vector3 offsetDir = new Vector3(0, 0, 0);
    Vector3 defaultTextPos = new Vector3();

    BrickManager brickManager;

    private void Start()
    {
        brickManager = GetComponent<BrickManager>();
        nameToIndex = ObjectDetector.DetectedLabelIdxToLabelName.ToDictionary(pair => pair.Value, pair => pair.Key);
    }


    private void Update()
    {
        if (runOnce)
        {
            GetRoom();
        }

        drawnObjects.ForEach(Destroy);
        drawnObjects.Clear();
        foreach (Transform t in canvas.transform)
        {
            Destroy(t.gameObject);
        }

        GameUtils.AddText(centerCam, canvas, "Running...", defaultTextPos + new Vector3(0, 0.1f, 0), Color.white, 2f);

        foreach (var l in brickManager.LifeTimeObjects.Values)
        {
            foreach (var obj in l)
            {
                if (obj.obj.activeSelf)
                {
                    GameUtils.AddText(centerCam, canvas, obj.labelName + ": " + obj.obj.transform.position.ToString() + "\nLifetime: " + obj.lifeTime, obj.obj.transform.position + new Vector3(0, 0.01f, 0), GameUtils.nameToColor[obj.labelName]);
                }
            }
        }
    }

    //private void FixedUpdate()
    //{
    //    foreach (var l in LifeTimeObjects.Values)
    //    {
    //        foreach (var obj in l)
    //        {
    //            obj.lifeTime -= Time.deltaTime;
    //        }
    //    }

    //    foreach (var l in LifeTimeObjects.Values)
    //    {
    //        for (int i = l.Count - 1; i >= 0; i--)
    //        {
    //            if (l[i].lifeTime <= 0)
    //            {
    //                Destroy(l[i].obj);
    //                l.RemoveAt(i);
    //            }
    //        }
    //    }
    //}

    //void OnObjectDetected(List<DetectedObject> detectedObjects)
    //{
    //    //debugObjects.ForEach(Destroy);
    //    //debugObjects.Clear();
    //    foreach (var v in detectedObjects)
    //    {
    //        //Debug.Log(v);
    //        if (LifeTimeObjects[v.labelName].Count == 0)
    //        {
    //            GameObject go = new GameObject();
    //            go.transform.position = v.worldPos;
    //            LifeTimeObjects[v.labelName].Add(new LifeTimeObject(2, go,v.labelName));
    //        }
    //        else
    //        {
    //            foreach (var l in LifeTimeObjects[v.labelName])
    //            {
    //                if (Vector3.Distance(l.obj.transform.position, v.worldPos) <= distanceThreshold)
    //                {
    //                    //GameObject go = v.DrawSmall();
    //                    //LifeTimeObjects[v.labelName].Add(new LifeTimeObject(5, go,v.labelName));
    //                    l.lifeTime = 1;
    //                }
    //            }
    //        }

    //    }
    //}
    public List<DetectedObject> GetDetectedObjects()
    {
        List<DetectedObject> res = new();
        foreach (var l in LifeTimeObjects.Values)
        {
            if (l.Count >= 0)
            {
                res.Add(new DetectedObject(nameToIndex[l[0].labelName], l[0].labelName, l[0].obj.transform.position));
            }
        }
        return res;
    }

    public List<LifeTimeObject> getlifeTimeObjects()
    {
        List<LifeTimeObject> res = new();
        foreach (var l in LifeTimeObjects.Values)
        {
            if (l.Count >= 0)
            {
                res.Add(l[0]);
            }
        }
        return res;
    }

    private void GetRoom()
    {
        room = MRUK.Instance.GetCurrentRoom();
        Debug.LogWarning(room != null);
        if (room != null)
        {
            anchors = room.Anchors;
            foreach (MRUKAnchor anchor in anchors)
            {
                if (anchor.Label == MRUKAnchor.SceneLabels.TABLE)
                {
                    anchorPoint = anchor.gameObject.transform.position;
                }
            }
            runOnce = false;
        }

        if (!runOnce)
        {
            offsetDir = (anchorPoint - new Vector3(centerCam.position.x, anchorPoint.y, centerCam.position.z)).normalized;

            defaultTextPos = anchorPoint + offsetDir * 0.2f;

        }
    }
}
