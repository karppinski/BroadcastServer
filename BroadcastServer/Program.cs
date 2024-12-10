using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace BroadcastServer
{
    class Program
    {
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
                    await ProcessRequest(context);
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
            Console.WriteLine("Client connected");

            byte[] buffer = new byte[1024];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if(result.MessageType == WebSocketMessageType.Text)
                {
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Reviced {receivedMessage}");

                    string responseMessage = $"Server received message {receivedMessage}";
                    var responseBuffer = Encoding.UTF8.GetBytes(responseMessage);
                    await webSocket.SendAsync(responseBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else if(result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Client disconected.");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }
    }
}
