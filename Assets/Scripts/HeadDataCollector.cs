using System.Text;
using PassthroughCameraSamples;
using UnityEngine;
using UnityEngine.Serialization;

public class HeadDataCollector : DataCollection
{
    public GameObject head;
    
    protected override void StartTracking()
    {
        
        var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(PassthroughCameraEye.Left);
        Log($"POS_FOCAL:{intrinsics.FocalLength};POS_PRINCIPAL:{intrinsics.PrincipalPoint}");
    }

    protected override void UpdateTracking()
    {
        Log($"POS_POSITION:{head.transform.position.ToString("F5")};POS_ROTATION:{head.transform.rotation}");
        base.UpdateTracking();
    }
}
