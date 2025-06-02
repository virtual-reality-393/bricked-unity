using UnityEngine;
using UnityEngine.Serialization;

public class BrickDataCollector : DataCollection
{
    public ObjectDetector objectDetector;  

    protected override void StartTracking()
    {
        objectDetector.OnObjectsDetected += OnObjectsDetected;
    }

    private void OnObjectsDetected(object sender, ObjectDetectedEventArgs e)
    {
        foreach (var obj in e.DetectedObjects)
        {
            if (IsPointWithinPlane(obj.worldPos))
            {
                Log($"{GetPlaneNormalizedCoordinates(obj.worldPos).ToString("F5")};{obj.labelName}");       
            }
        }
    }
}
