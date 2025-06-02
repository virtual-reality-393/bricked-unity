using System;
using System.Collections;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class DataCollection : MonoBehaviour
{
    protected static MRUKAnchor TableAnchor;
    protected static MRUKRoom Room;
    protected static Rect TablePlane;
    
    protected int frameNum;
    [SerializeField] protected string identifier;


    private static bool _isEnsuring;

    private bool _firstTrack = true;

    private void GetRoom()
    {
        Room = MRUK.Instance?.GetCurrentRoom();
        if (Room)
        {
            var anchors = Room.Anchors;
            foreach (MRUKAnchor anchor in anchors)
            {
                if (anchor.Label == MRUKAnchor.SceneLabels.TABLE)
                {
                    TableAnchor = anchor;
                    TablePlane = (Rect)TableAnchor.PlaneRect;
                    DataLogger.Log("tablePlane", $"{TablePlane.width.ToString()},{TablePlane.height.ToString()}");
                }
            }
        }
    }

    private void Awake()
    {
        if (!_isEnsuring)
        {
            _isEnsuring = true;
            StartCoroutine(EnsureTracking());
        }
    }


    IEnumerator EnsureTracking()
    {
        while(!Room)
        {
            GetRoom();
            yield return null;
        }
    }

    private void Update()
    {
        if (!Room) return;
        
        if (_firstTrack)
        {
            StartTracking();
            _firstTrack = false;
        }
            
        UpdateTracking();
    }

    protected virtual void StartTracking() {}

    protected virtual void UpdateTracking()
    {
        frameNum++;
        
    }

    protected void Log(string message)
    {
        DataLogger.Log($"{identifier}",message);
    }

    protected static bool IsPointWithinPlane(Vector3 position)
    {
        return IsPointWithinPlane(GetPlaneCoordinates(position));
    }
    
    protected static bool IsPointWithinPlane(Vector2 position)
    {
        return TablePlane.Contains(new Vector2(position.x, position.y));
    }

    protected static Vector2 GetPlaneCoordinates(Vector3 position)
    {
        position.y = TableAnchor.transform.position.y;
        var localSpace = TableAnchor.transform.InverseTransformPoint(position);
        return new Vector2(localSpace.x,localSpace.y);
    }

    public static Vector2 GetPlaneNormalizedCoordinates(Vector3 position)
    {
        if (TablePlane.height <= 0)
            return new Vector2(-1, -1);
            
        var localSpace = GetPlaneCoordinates(position);
        
        var normalizedPosition = new Vector2((localSpace.x-TablePlane.xMin)/TablePlane.width,(localSpace.y-TablePlane.yMin)/TablePlane.height);
        
        return normalizedPosition;
    }
}