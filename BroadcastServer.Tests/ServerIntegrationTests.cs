using NuGet.Frameworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace BroadcastServer.Tests
{
    public  class ServerIntegrationTests
    {
        [Fact]
        public void Server_StartsListener_AndListenOnDefaultPort()
        {
            //Arrange
            using var httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://localhost:7000/");

            //Act

            httpListener.Start();

            //Assert
            Assert.True( httpListener.IsListening);
        }

        [Theory]
        [MemberData(nameof(GetPortRange))]
        public void Server_StartsListener_OnPorts7001to7099ifDefaultIsTaken(int port)
        {
            //Arragne
            using var httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:7000/");
            httpListener.Start();

            using var httpListener2 = new HttpListener();
            httpListener2.Prefixes.Add($"http://localhost:{port}/");

            Exception? ex = null;


            //Act
            try
            {
                httpListener2.Start();  
            }
            catch(HttpListenerException e)  
            {
                ex = e;
            }

            //Assert
            if (port == 7000)
            {
                Assert.NotNull(ex);
            }
            else
            {
                Assert.Null(ex);
                Assert.True(httpListener2.IsListening);
            }



        }
        public static IEnumerable<object[]> GetPortRange()
        {
            return Enumerable.Range(7000, 100)
                            .Select(port => new object[] { port });
        }

        [Theory]
        [MemberData(nameof(GetPortRange))]
        public async Task Server_ConnectsToWebSocket_OnEachPortItShouldOpenWebSocketConnection(int port)
        {
            using var httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://localhost:{port}/");
            httpListener.Start();

            using var webSocketClient = new ClientWebSocket();
            using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var token = cancellationSource.Token;

            try
            {
                // Create a task to wait for the server to be ready
                var serverReadyTask = Task.Run(() =>
                {
                    while (!httpListener.IsListening)
                    {
                        Thread.Sleep(10);
                    }
                }, token);

                // Wait for the server to be ready before connecting
                await serverReadyTask;

                // Separate tasks for server and client operations
                var serverTask = Task.Run(async () =>
                {
                    var context = await httpListener.GetContextAsync();
                    return await context.AcceptWebSocketAsync(null);
                }, token);

                // Connect client with a separate timeout
                await webSocketClient.ConnectAsync(new Uri($"ws://localhost:{port}"), token);

                // Wait for server WebSocket context
                var webSocketContext = await serverTask;

                // Assert connection state
                Assert.Equal(WebSocketState.Open, webSocketClient.State);
                webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "", token);

            }
            catch (Exception e)
            {
                Console.WriteLine($"Error connecting to WebSocket on port {port}: {e.Message}");
                throw; 
            }
            finally
            {
                httpListener.Stop();
            }
        }



    }


}
