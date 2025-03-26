using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using UnityEngine;

public class PlaneSpwaner : MonoBehaviour
{

    public MRUK MRUK;

    MRUKRoom room;
    List<MRUKAnchor> anchors = new List<MRUKAnchor>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        room = MRUK.GetCurrentRoom();
        anchors = room.Anchors;
        Debug.Log("Nr. anchors: " + anchors.Count);
        foreach (MRUKAnchor anchor in anchors)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.position = anchor.gameObject.transform.position;
            plane.transform.rotation = Quaternion.identity;
            plane.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
