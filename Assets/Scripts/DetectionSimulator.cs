//
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
// using Random = UnityEngine.Random;
//
// public class DetectionSimulator : MonoBehaviour
// {
//     [Header("Simulator Settings")]
//     public List<Triplet<string,float,Transform>> objectsToSimulate = new();
//     public float uncertainty = 0.05f;
//     public int framesBetweenDetections = 25;
//     private Dictionary<string, int> nameToIndex;
//     private List<GameObject> debugObjects = new();
//     public Dictionary<string,List<LifeTimeObject>> LifeTimeObjects = new();
//     
//     [Header("Detection Settings")]
//     public float distanceThreshold = 0.4f;
//     
//     void Start()
//     {
//         nameToIndex = ObjectDetector.DetectedLabelIdxToLabelName.ToDictionary(pair => pair.Value, pair => pair.Key);
//         foreach (var labelName in ObjectDetector.DetectedLabelIdxToLabelName.Values)
//         {
//             LifeTimeObjects.Add(labelName,new List<LifeTimeObject>());
//         }
//
//         StartCoroutine(StartSimulation());
//     }
//
//     IEnumerator StartSimulation()
//     {
//         while (true)
//         {
//             int frames = 0;
//             while (frames != framesBetweenDetections)
//             {
//                 frames += 1;
//                 yield return new WaitForFixedUpdate();
//             }
//
//             var res = new List<DetectedObject>();
//             foreach (var (labelName, falloutChance, position) in objectsToSimulate.Select(x => (x.val1, x.val2,x.val3.position)))
//             {
//                 if (Random.value >= falloutChance)
//                 {
//                     res.Add(new DetectedObject(nameToIndex[labelName],labelName,position + Vector3.right * ((Random.value-0.5f) * uncertainty)+Vector3.up * ((Random.value-0.5f) * uncertainty)+Vector3.forward * ((Random.value-0.5f) * uncertainty)));
//                 }
//             }
//             
//             OnObjectDetected(res);
//             
//             yield return new WaitForFixedUpdate();
//         }
//     }
//
//
//     void OnObjectDetected(List<DetectedObject> detectedObjects)
//     {
//         debugObjects.ForEach(Destroy);
//         debugObjects.Clear();
//         foreach (var v in detectedObjects)
//         {
//             Debug.Log(v);
//             if (LifeTimeObjects[v.labelName].Count == 0)
//             {
//                 GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
//                 go.transform.position = v.worldPos;
//                 LifeTimeObjects[v.labelName].Add(new LifeTimeObject(5,go));
//             }
//             else
//             {
//                 foreach (var l in LifeTimeObjects[v.labelName])
//                 {
//                     if (Vector3.Distance(l.obj.transform.position, v.worldPos) <= distanceThreshold)
//                     {
//                         GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
//                         go.transform.position = v.worldPos;
//                         LifeTimeObjects[v.labelName].Add(new LifeTimeObject(1,go));
//                     }
//                     else
//                     {
//                         l.lifeTime = 1;
//                     }
//                 }    
//             }
//             
//         }
//     }
//
//
//     private void FixedUpdate()
//     {
//         foreach (var l in LifeTimeObjects.Values)
//         {
//             foreach (var obj in l)
//             {
//                 obj.lifeTime -= Time.deltaTime;
//             }
//         }
//
//         foreach (var l in LifeTimeObjects.Values)
//         {
//             for (int i = l.Count-1;i>=0; i--)
//             {
//                 if (l[i].lifeTime <= 0)
//                 {
//                     l.RemoveAt(i);
//                 }
//             }
//         }
//     }
// }
//
