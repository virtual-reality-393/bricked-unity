using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Net.WebSockets;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

public class PythonServer
{
    private ClientWebSocket webSocket;

    public bool started => webSocket.State == WebSocketState.Open;

    public bool closed => webSocket.State == WebSocketState.Closed;

    // URL of the WebSocket server
    private string serverUri = "ws://192.168.1.103:8765";

    // Start is called before the first frame update
    public async Task StartServer()
    {
        // Create the WebSocket client
        webSocket = new ClientWebSocket();

        // Connect to the WebSocket server
        await ConnectWebSocket();
    }

    // Method to connect to the WebSocket server
    private async Task ConnectWebSocket()
    {
        try
        {
            await webSocket.ConnectAsync(new Uri(serverUri), CancellationToken.None);
            Debug.Log("WebSocket connected!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"WebSocket connection error: {ex.Message}");
        }
    }

    // Method to send a message to the WebSocket server
    public async Task SendMessage(string message)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            
        }
    }

    // Method to receive messages from the WebSocket server
    public async Task<List<string>> ReceiveMessages()
    {
        byte[] buffer = new byte[1024];

        List<string> list = new();

        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Text)
        {
            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
            list.Add(receivedMessage);
        }

        return list;
    }

    // Method to close the WebSocket connection when the app is quitting
    public async Task OnApplicationQuit()
    {
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
        webSocket.Dispose();
        Debug.Log("WebSocket connection closed.");
    }
}
