using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

public class HandDataCollector : DataCollection
{
    public OVRSkeleton hand;    
    private GameObject _hand;
    public StringBuilder sb;
    public bool isRightHand;
    
    protected override void StartTracking()
    {
        _hand = Instantiate(GameManager.Instance.cubePrefab, hand.transform.position, Quaternion.identity);
        _hand.GetComponent<Renderer>().material.color = Color.red;
        sb = new StringBuilder();
    }

    protected override void UpdateTracking()
    {
        sb.Clear();
        Vector3 tableHandPos = hand.transform.position + hand.transform.right * (isRightHand ? -0.09f : 0.09f);
        tableHandPos.y = TableAnchor.transform.position.y;
        _hand.transform.position = tableHandPos;
         
        if (IsPointWithinPlane(tableHandPos))
        {
            _hand.GetComponent<Renderer>().material.color = Color.green;
        }
        else
        {
            _hand.GetComponent<Renderer>().material.color = Color.red;
        }

        sb.Append($"POS_PALM:{GetPlaneNormalizedCoordinates(tableHandPos).ToString("F5")}");
        
        for (var index = 0; index < hand.Bones.Count; index++)
        {
            sb.Append(";");
            var bone = hand.Bones[index];
            sb.Append($"POS_BONE{index}:{bone.Transform.position.ToString("F5")}");
        }
        
        Log(sb.ToString());
        base.UpdateTracking();
    }
}
