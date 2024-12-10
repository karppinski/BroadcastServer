using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace BroadcastClient
{
    class Program
    {
        static async Task Main()
        {
            var clientWebSocket = new ClientWebSocket();

            int maxRetry = 6;
            int retryCount = 1;

            while (retryCount < maxRetry)
            {
                try
                {
                    await clientWebSocket.ConnectAsync(new Uri("ws://localhost:7000/"), CancellationToken.None);
                    Console.WriteLine("Connected to the server!");

                    var receivingTask = ReceiveMessagesAsync(clientWebSocket);


                    while (clientWebSocket.State == WebSocketState.Open)
                    {


                        Console.WriteLine("Write a message or write exit to close):");
                        string message = Console.ReadLine();

                        if (message == "exit")
                        {
                            Console.WriteLine("Closing connection");
                            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client exited", CancellationToken.None);
                            break;
                        }

                        var messageToSend = Encoding.UTF8.GetBytes(message);

                        await clientWebSocket.SendAsync
                            (new ArraySegment<byte>(messageToSend)
                            , WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);


                    }

                    await receivingTask;

                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Server is down or stopped connection");
                    Console.WriteLine("Retry attempt no " + retryCount);
                    retryCount++;
                    await Task.Delay(3000);
                    if(retryCount == maxRetry)
                    {
                        Console.WriteLine($"Exception message {ex}");
                    }
                }
            }
            Console.WriteLine("There is a problem with server, try to restart both apps.");

        }

        static async Task ReceiveMessagesAsync(ClientWebSocket clientWebSocket)
        {

            byte[] buffer = new byte[1024];

            while(clientWebSocket.State == WebSocketState.Open)
            {
                var result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                

                if(result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine(message);
                }
                else if(result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Server closed your connection");
                    await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }

            }

        }
    }
}
