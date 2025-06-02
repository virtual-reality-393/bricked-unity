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
        Log($"FOCAL:{intrinsics.FocalLength};PRINCIPAL:{intrinsics.PrincipalPoint}");
    }

    protected override void UpdateTracking()
    {
        Log($"POSITION:{head.transform.position.ToString("F5")};ROTATION:{head.transform.rotation}");
        base.UpdateTracking();
    }
}
