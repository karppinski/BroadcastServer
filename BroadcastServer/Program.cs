using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace BroadcastServer
{
    class Program
    {
        private static ConcurrentDictionary<Guid, WebSocket> _connections = new();
        static async Task Main(string[] args)
        {
            int defaultPort = 7000;

            if (args.Length != 2 || args[0] != "--port" || args[1].Length != 4)
            {
                    Console.WriteLine("Connecting to the default port.");
            }
            else if (int.TryParse(args[1], out int port))
            {
                if (port >= 7000 || port <= 7099)
                {
                    defaultPort = port;
                }
                else
                {
                    Console.WriteLine("This application uses ports between 7000 to 7099");
                    Console.WriteLine("You will be connected to default port.");
                }
            }

            using var httpListener = new HttpListener();

            int workingPort = CheckPortAvaliability(defaultPort);
            httpListener.Prefixes.Add($"http://localhost:{workingPort}/");
            httpListener.Start();
            Console.WriteLine("Server set and waiting for connections!");



            while (true) 
            {
                var  context = await httpListener.GetContextAsync();
                if(context.Request.IsWebSocketRequest)
                {
                    var clientId = Guid.NewGuid();
                    _ = Task.Run(() => ProcessRequest(context, clientId));
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }


        }

        static async Task ProcessRequest(HttpListenerContext context, Guid clientId)
        {
            WebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
            using var webSocket = webSocketContext.WebSocket;

            Console.WriteLine($"Client id {clientId} connected");

            var clientCancellationTokenSource = new CancellationTokenSource();
            var clientCancellationToken = clientCancellationTokenSource.Token;
            _ = Task.Run(() => ClientCanellationMonitorAsync(clientId, clientCancellationToken));

            _connections.TryAdd(clientId, webSocket);
            clientCancellationTokenSource.CancelAfter(5000);

            byte[] buffer = new byte[1024];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), clientCancellationToken);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        if (receivedMessage == "refresh")
                        {
                            //Console.WriteLine("Token refreshed");
                            clientCancellationTokenSource.CancelAfter(15000);
                        }
                        else
                        {
                            await BroadcastMessageAsync(clientId, receivedMessage, clientCancellationToken);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Client disconnected.");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Client {clientId} timed out.");
            }
            finally
            {
                _connections.TryRemove(clientId, out _);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
   
                Console.WriteLine($"Client {clientId} disconnected.");
            }
        }
    

        static async Task BroadcastMessageAsync(Guid senderId, string message, CancellationToken cancellationToken)
        {
            string responseMessage = $"Client {senderId}: {message}";
            var responseBuffer= Encoding.UTF8.GetBytes(responseMessage);

            foreach (var (id, webSocket) in _connections)
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        await webSocket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, cancellationToken);
                    }
                    catch (WebSocketException ex)
                    {
                        Console.WriteLine($"Error sending message to client {id}: {ex.Message}");
                        _connections.TryRemove(id, out _);
                    }
                }
            }
        }

        static async Task ClientCanellationMonitorAsync(Guid clientId, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch(TaskCanceledException)
            {
                    Console.WriteLine($"Client {clientId} has quit.");
            }
        }
        static int CheckPortAvaliability(int port)
        {
            while (true)
            {
                var httpListener = new HttpListener();
                try
                {
                    httpListener.Prefixes.Add($"http://localhost:{port}/");
                    httpListener.Start();
                    httpListener.Close();
                    break;
                }
                catch (HttpListenerException)
                {
                    port++;
                }
            }
            return port;
        }
    }
}
