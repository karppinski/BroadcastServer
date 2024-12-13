using System.Collections.Concurrent;
using System.ComponentModel.Design;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace BroadcastClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int maxRetry = 6;
            int retryCount = 1;


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

            int finalPort = await CheckIfServerRuningOnPort(defaultPort);
            if( finalPort == 7100)
            {
                Console.WriteLine("No running server found, try again later");
                Environment.Exit(0);

            }
            else if(finalPort == 7101)
            {
                Console.WriteLine("Shutting application down.");
                Environment.Exit(0);
            }

            while (retryCount < maxRetry)
            {

                using var clientWebSocket = new ClientWebSocket();


                using var clientCancellationSource = new CancellationTokenSource();
                var clientCancellationToken = clientCancellationSource.Token;
                try
                {
                   // tutaj zrobić porty dla klienta !!
                    await clientWebSocket.ConnectAsync(new Uri($"ws://localhost:/{finalPort}"), clientCancellationToken);
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

        static async Task<int> CheckIfServerRuningOnPort(int port)
        {
            while (port != 7100)
            {
                var clientWebSocket = new ClientWebSocket();
                try
                {
                    Console.WriteLine("Trying to connect on port " + port);
                    await clientWebSocket.ConnectAsync(new Uri($"ws://localhost:{port}/"), CancellationToken.None);
                    break;
                }
                catch (WebSocketException)
                {
                    Console.WriteLine($"Server with port {port} not found, do you want to try next port? [y/n]");
                    var key = Console.ReadKey();
                    if (key.KeyChar == 'y' || key.KeyChar == 'Y')
                    { port++; }
                    else { port = 7101; }
                }
            }
            return port;
        }

    }
}
