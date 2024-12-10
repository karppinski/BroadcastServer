using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace BroadcastServer
{
    class Program
    {
        private static ConcurrentDictionary<Guid, WebSocket> _connections = new();
        static async Task Main()
        {
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:7000/");
            httpListener.Start();
            Console.WriteLine("Server set and waiting for connections!");

            while (true)
            {
                var  context = await httpListener.GetContextAsync();
                if(context.Request.IsWebSocketRequest)
                {
                    _ = Task.Run(() => ProcessRequest(context));
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

        static async Task ProcessRequest(HttpListenerContext context)
        {
            WebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;
            var clientId = Guid.NewGuid();
            Console.WriteLine($"Client id {clientId} connected");

            _connections.TryAdd(clientId, webSocket);

            byte[] buffer = new byte[1024];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if(result.MessageType == WebSocketMessageType.Text)
                {
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Reviced {receivedMessage} from {clientId}");

                    await BroadcastMessageAsync(clientId, receivedMessage);
                }
                else if(result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Client disconected.");
                    _connections.TryRemove(clientId, out _);
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }

        static async Task BroadcastMessageAsync(Guid senderId, string message)
        {
            string responseMessage = $"Client {senderId}: {message}";
            var responseBuffer= Encoding.UTF8.GetBytes(responseMessage);

            foreach (var (id, webSocket) in _connections)
            {
                if(webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        await webSocket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch
                    {
                        Console.WriteLine($"There is a problem with sending message to {id}");
                    }
                }
            }
            

        }
    }
}
