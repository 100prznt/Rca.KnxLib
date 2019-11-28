using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Rca.KnxIpLib
{
    public class Sandbox
    {
        //Sample code from: https://docs.microsoft.com/de-de/windows/uwp/networking/sockets

        // Every protocol typically has a standard port number. For example, HTTP is typically 80, FTP is 20 and 21, etc.
        // For this example, we'll choose different arbitrary port numbers for client and server, since both will be running on the same machine.
        static string ClientPortNumber = "1336";
        static string ServerPortNumber = "1337";

        public Sandbox()
        {
            StartServer();
            StartClient();
        }

        private async void StartServer()
        {
            try
            {
                var serverDatagramSocket = new Windows.Networking.Sockets.DatagramSocket();

                // The ConnectionReceived event is raised when connections are received.
                serverDatagramSocket.MessageReceived += ServerDatagramSocket_MessageReceived;

                Debug.WriteLine("server is about to bind...");

                // Start listening for incoming TCP connections on the specified port. You can specify any port that's not currently in use.
                await serverDatagramSocket.BindServiceNameAsync(ServerPortNumber);

                Debug.WriteLine(string.Format("server is bound to port number {0}", ServerPortNumber));
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                Debug.WriteLine(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
        }

        private async void ServerDatagramSocket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender, Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
        {
            string request;
            using (DataReader dataReader = args.GetDataReader())
            {
                request = dataReader.ReadString(dataReader.UnconsumedBufferLength).Trim();
            }

            Debug.WriteLine(string.Format("server received the request: \"{0}\"", request));

            // Echo the request back as the response.
            using (Stream outputStream = (await sender.GetOutputStreamAsync(args.RemoteAddress, ClientPortNumber)).AsStreamForWrite())
            {
                using (var streamWriter = new StreamWriter(outputStream))
                {
                    await streamWriter.WriteLineAsync(request);
                    await streamWriter.FlushAsync();
                }
            }

            Debug.WriteLine(string.Format("server sent back the response: \"{0}\"", request));

            sender.Dispose();

            Debug.WriteLine("server closed its socket");
        }

        private async void StartClient()
        {
            try
            {
                // Create the DatagramSocket and establish a connection to the echo server.
                var clientDatagramSocket = new Windows.Networking.Sockets.DatagramSocket();

                clientDatagramSocket.MessageReceived += ClientDatagramSocket_MessageReceived;

                // The server hostname that we will be establishing a connection to. In this example, the server and client are in the same process.
                var hostName = new Windows.Networking.HostName("localhost");

                Debug.WriteLine("client is about to bind...");

                await clientDatagramSocket.BindServiceNameAsync(ClientPortNumber);

                Debug.WriteLine(string.Format("client is bound to port number {0}", ClientPortNumber));

                // Send a request to the echo server.
                string request = "Hello, World!";
                using (var serverDatagramSocket = new Windows.Networking.Sockets.DatagramSocket())
                {
                    using (Stream outputStream = (await serverDatagramSocket.GetOutputStreamAsync(hostName, ServerPortNumber)).AsStreamForWrite())
                    {
                        using (var streamWriter = new StreamWriter(outputStream))
                        {
                            await streamWriter.WriteLineAsync(request);
                            await streamWriter.FlushAsync();
                        }
                    }
                }

                Debug.WriteLine(string.Format("client sent the request: \"{0}\"", request));
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                Debug.WriteLine(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
        }

        private async void ClientDatagramSocket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender, Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
        {
            string response;
            using (DataReader dataReader = args.GetDataReader())
            {
                response = dataReader.ReadString(dataReader.UnconsumedBufferLength).Trim();
            }

            Debug.WriteLine(string.Format("client received the response: \"{0}\"", response));

            sender.Dispose();

            Debug.WriteLine("client closed its socket");
        }



    }
}
