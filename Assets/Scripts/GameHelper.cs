using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

public class GameHelper : MonoBehaviour
{
    private PythonServer server;
    public bool serverStarted => server.started;
    public bool serverClosed => server.closed;
    async void Start()
    {
        server = new PythonServer();
        await server.StartServer();
    }

    private T HandleJSON<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }

    private async Task<List<Brick>> GetBricksAsync()
    {
        await server.SendMessage("get_bricks");
        var result = await server.ReceiveMessages();

        return HandleJSON<List<Brick>>(result[0]);
    }

    public Task<List<Brick>> GetBricks()
    {
        return GetBricksAsync();
    }

    private async Task<List<Stack>> GetStacksAsync()
    {
        await server.SendMessage("get_stacks");
        var result = await server.ReceiveMessages();

        return HandleJSON<List<Stack>>(result[0]);
    }

    public List<Stack> GetStacks()
    {
        return GetStacksAsync().Result;
    }

    private async void OnApplicationQuit()
    {
        await server.OnApplicationQuit();
    }
}

//[Serializable]
//public class Brick
//{
//    public string color;
//    public int[] box;
//    public int[] center;
//    public bool in_stack;


//    public override string ToString()
//    {
//        return $"({color},{string.Join(",", box)},{string.Join(",", center)},{in_stack})";
//    }
//}

[Serializable]
public class Stack
{
    public Brick[] bricks;
    public int[] box;
}