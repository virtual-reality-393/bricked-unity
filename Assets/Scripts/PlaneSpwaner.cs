using Meta.XR.MRUtilityKit;
using PassthroughCameraSamples;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlaneSpwaner : MonoBehaviour
{

    //public MRUK mruk;
    public GameObject objectHolder;
    public Transform camRig;
    MRUKRoom room;
    List<MRUKAnchor> anchors = new();

    MRUKAnchor currentTabel = new MRUKAnchor();

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
                    plane.transform.rotation = Quaternion.Euler(anchor.gameObject.transform.localRotation.eulerAngles + new Vector3(-90,0,-180));
                    plane.transform.localScale =new Vector3(bounds.size.x, 1, bounds.size.z)/10;
                    plane.transform.parent = anchor.transform;
                    plane.GetComponent<Renderer>().material.color = Color.red;

                    currentTabel = anchor;
                }
            

            }

            b = false;
        }

        if (!b && room != null)
        {
            List<float> dists = new();
            foreach (MRUKAnchor anchor in anchors)
            {
                if (anchor.Label == MRUKAnchor.SceneLabels.TABLE)
                {
                    float dist = Vector3.Distance(camRig.position, anchor.transform.position);
                    dists.Add(dist);
                    anchor.transform.GetChild(1).GetComponent<Renderer>().material.color = Color.red;
                }
                else
                {
                    dists.Add(10000);
                }
            }

            int i = dists.IndexOf(dists.Min());
            currentTabel = anchors[i];
            currentTabel.transform.GetChild(1).GetComponent<Renderer>().material.color = Color.green;
        }




    }
}
