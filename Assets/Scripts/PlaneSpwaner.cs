using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using UnityEngine;

public class PlaneSpwaner : MonoBehaviour
{

    //public MRUK mruk;
    public GameObject objectHolder;
    MRUKRoom room;
    List<MRUKAnchor> anchors = new();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {


    }

    bool b = true;
    // Update is called once per frame
    void Update()
    {
        room = MRUK.Instance.GetCurrentRoom();

        if (room != null && b)
        {
            anchors = room.Anchors;
            foreach (MRUKAnchor anchor in anchors)
            {
                if (anchor.Label == MRUKAnchor.SceneLabels.TABLE)
                {
                    Bounds bounds = (Bounds)anchor.VolumeBounds;
                    bounds.Expand(-0.2f);
                    GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    plane.transform.position = anchor.gameObject.transform.position;
                    plane.transform.rotation = Quaternion.identity;
                    plane.transform.localScale =new Vector3(bounds.size.z, 1, bounds.size.x)/10;
                    plane.transform.parent = objectHolder.transform;
                    plane.GetComponent<Renderer>().material.color = Color.green;
                }
            }

            b = false;
        }


    }
}
