// using System.Collections.Generic;
// using UnityEngine;
//
// public class JacoDOTest : MonoBehaviour
// {
//
//     public Dictionary<string, int> dict = new Dictionary<string, int> { { "red", 1 }, { "green", 3 }, { "blue", 2 }, { "yellow", 3 }, { "magenta", 0 } };
//
//     public bool b = true;
//
//     // Start is called once before the first execution of Update after the MonoBehaviour is created
//     void Start()
//     {
//
//     }
//
//     // Update is called once per frame
//     void Update()
//     {
//         if (b)
//         {
//             foreach (Transform t in transform)
//             {
//                 Destroy(t.gameObject);
//             }
//
//             List<string> sorted = GameUtils.GenerateListFromDict(dict);
//             List<string> stack = GameUtils.GenetateStack(sorted);
//
//             Vector3 pos = new Vector3(0, 1, 0);
//
//             for (int i = 0; i < stack.Count; i++)
//             {
//                 GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
//                 cube.GetComponent<Renderer>().material.color = GameUtils.GetColorByName(stack[i]);
//                 cube.transform.parent = transform;
//                 cube.transform.position = pos * i;
//             }
//             b = false;
//         }
//     }
// }
