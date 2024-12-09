using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace BroadcastClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var clientWebSocket = new ClientWebSocket();

            try
            {
                await clientWebSocket.ConnectAsync(new Uri("ws://localhost:7000/"), CancellationToken.None);

                while (clientWebSocket.State == WebSocketState.Open)
                {
                    Console.WriteLine("Write message");
                    string message = Console.ReadLine();

                    if(message == "exit")
                    {
                        Console.WriteLine("Closing connection");
                        await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }

                    var messageToSend = Encoding.UTF8.GetBytes(message);

                    await clientWebSocket.SendAsync
                        (new ArraySegment<byte>(messageToSend)
                        , WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"There is a problem {ex}");
            }
        }
    }
}
