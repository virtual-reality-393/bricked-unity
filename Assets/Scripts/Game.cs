//using System.Collections.Generic;
//using System.Threading.Tasks;
//using UnityEngine;
//using System.Collections.Concurrent;
//using System.Threading;
//using UnityEngine.UI;

//public class Game : MonoBehaviour
//{
//    public GameHelper helper;
//    public Camera cam;
//    public List<Brick> bricks_now;

//    public List<GameObject> hitObjects;

//    public ConcurrentStack<List<Brick>> commStack;

//    public GameObject imgPrefab;
//    public Canvas canvas;

//    bool killSignal;

//    public Dictionary<string, Color> nameToColor = new Dictionary<string, Color>()
//    {
//        {"green", Color.green },
//        {"blue", Color.blue },
//        {"yellow", Color.yellow},
//        {"red", Color.red},
//    };



//    void Start()
//    {
//        commStack = new ConcurrentStack<List<Brick>>();
//        bricks_now = new List<Brick>();

//        Thread serverThread = new Thread(new ThreadStart(ServerThread));

//        serverThread.Start();

//    }

//    async void ServerThread()
//    {
//        while (!killSignal)
//        {
//            if (helper.serverStarted)
//            {
//                commStack.Push(await helper.GetBricks());
//            }
//        }
//    }

//    void Update()
//    {
//        Debug.Log(Screen.width);
//        Debug.Log(Screen.height);

        

//        if (commStack.TryPop(out List<Brick> bricks))
//        {
//            foreach (var obj in hitObjects)
//            {
//                Destroy(obj);
//            }
//            hitObjects = new List<GameObject>();
//            bricks_now = bricks;
//            foreach (var obj in bricks)
//            {
//                HandleBrick(obj);
//            }
//        }

//        foreach(var brick in bricks_now)
//        {
//            Ray ray = cam.ScreenPointToRay(new Vector3(brick.center[0], Screen.height - brick.center[1], 0));
//            Debug.DrawRay(ray.origin, ray.direction * 10, Color.magenta);
//        }
//    }


//    void HandleBrick(Brick brick)
//    {
//        Ray ray = cam.ScreenPointToRay(new Vector3(brick.center[0], Screen.height-brick.center[1],0));

//        //var img = Instantiate(imgPrefab);

//        //img.transform.parent = canvas.transform;

//        //img.transform.position = new Vector3(brick.center[0], brick.center[1], 0);

//        //hitObjects.Add(img);

//        if (Physics.Raycast(ray, out RaycastHit hit, 1000))
//        {
//            var newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);

//            newObj.transform.position = hit.point;

//            Destroy(newObj.GetComponent<Collider>());

//            newObj.transform.localScale = Vector3.one / 35f;

//            hitObjects.Add(newObj);





//        }
//    }

//    void OnApplicationQuit()
//    {
//        killSignal = true;
//    }
//}
