using System;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using UnityEngine;

public class PlaneSpwaner : MonoBehaviour
{
    public delegate void PlaneSpawnEventHandler(PlaneSpawnedEventArgs plane);

    public EventHandler<PlaneSpawnedEventArgs> OnPlaneSpawned;
    //public MRUK mruk;
    public GameObject objectHolder;
    MRUKRoom room;
    List<MRUKAnchor> anchors = new();

    private bool test;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {
        if (!test)
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
                        Vector2 bounds = anchor.PlaneRect.Value.size;
                        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        plane.transform.position = anchor.gameObject.transform.position;
                        plane.transform.rotation = Quaternion.Euler(anchor.gameObject.transform.localRotation.eulerAngles + new Vector3(-90,0,-180));
                        plane.transform.localScale =new Vector3(bounds.x, 1, bounds.y)/10;
                        plane.transform.parent = objectHolder.transform;
                        plane.GetComponent<Renderer>().material.color = Color.green;
                        OnPlaneSpawned?.Invoke(this,new PlaneSpawnedEventArgs(plane));
                    }
                }

                test = true;
            }
            
        }
    }
}


public class PlaneSpawnedEventArgs : EventArgs
{
    public GameObject Plane;

    public PlaneSpawnedEventArgs(GameObject plane)
    {
        Plane = plane;
    }
}
