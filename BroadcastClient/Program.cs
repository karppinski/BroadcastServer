using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace BroadcastClient
{
    class Program
    {
        static async Task Main()
        {
            int maxRetry = 6;
            int retryCount = 1;



            while (retryCount < maxRetry)
            {
                using var clientWebSocket = new ClientWebSocket();
                using var clientCancellationSource = new CancellationTokenSource();
                var clientCancellationToken = clientCancellationSource.Token;
                try
                {
                  
                    await clientWebSocket.ConnectAsync(new Uri("ws://localhost:7000/"), clientCancellationToken);
                    Console.WriteLine("Connected to the server!");
                    Console.WriteLine();
                    Console.WriteLine("Write a message or write 'exit' to close:");


                    var receivingTask = ReceiveMessagesAsync(clientWebSocket, clientCancellationToken);
                    var cancellationRefresherTask = SendCancellationTokenRefresherAsync(clientWebSocket, clientCancellationToken);


                    while (clientWebSocket.State == WebSocketState.Open)
                    {
                        if (Console.KeyAvailable) // tutaj moze rozdzielic i dolnego catch zlapac jako iscancellationrequested
                        {
                            string message = Console.ReadLine();


                            if (message == "exit")
                            {
                                Console.WriteLine("Closing connection");
                                await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client exited", clientCancellationToken); // chyba tutaj pownien byc cancellation.none
                                clientCancellationSource.Cancel();
                                break;
                            }


                            var messageToSend = Encoding.UTF8.GetBytes(message);
                            await clientWebSocket.SendAsync(new ArraySegment<byte>(messageToSend), WebSocketMessageType.Text, true, clientCancellationToken);
                        }
                    }
                    await receivingTask;
                    Console.WriteLine("receiving oczekany");
                    await cancellationRefresherTask;
                    Console.WriteLine("cancellation refresher oczekany");
                }

                catch (OperationCanceledException)
                {
                    Console.WriteLine("Your exit has been proceeded.");
                    break;
                }
                catch (WebSocketException)
                {
                    clientCancellationSource.Cancel();
                    Console.WriteLine("Server is down or stopped connection");
                    Console.WriteLine("Retry attempt no " + retryCount);
                    retryCount++;
                    await Task.Delay(3000);
                    

                    if (retryCount == maxRetry)
                    {
                        Console.WriteLine("There is a problem with the server, try to restart both apps.");
                        break;
                    }
                }
            }
        }

        static async Task ReceiveMessagesAsync(ClientWebSocket clientWebSocket, CancellationToken clientCancellationToken)
        {
            byte[] buffer = new byte[1024];

            while (clientWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), clientCancellationToken);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Server closed your connection");
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Connection lost");
                    break;
                }
            }
        }

        static async Task SendCancellationTokenRefresherAsync(ClientWebSocket clientWebSocket, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var refreshToken = Encoding.UTF8.GetBytes("refresh");

                    await clientWebSocket.SendAsync(new ArraySegment<byte>(refreshToken), WebSocketMessageType.Text, true, cancellationToken);
                    await Task.Delay(4000, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Senc refresher task has been paused");
                    break;
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to send refresh token");
                    break;
                }
            }
        }

    }
}
