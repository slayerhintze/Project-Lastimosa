using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Project_Lastimosa
{
    public class Program
    {
        static void Main(string[] args)
        {
            // First let's do some testing to get the WebSocket server up and running
            // These two constants are values outlined in the README.md for this project, and as such should not be tampered with.
            string LIVE_API_IP_ADDRESS = "127.0.0.1";
            int LIVE_API_PORT = 443;

            using TcpListener liveAPIConnection = new TcpListener(IPAddress.Parse(LIVE_API_IP_ADDRESS), LIVE_API_PORT);
            liveAPIConnection.Start();

            Console.WriteLine($"Opened WebSocket server on IP address: {LIVE_API_IP_ADDRESS} and port: {LIVE_API_PORT}");

            TcpClient liveAPIClient = liveAPIConnection.AcceptTcpClient();

            Console.WriteLine("Client connected");

            using (NetworkStream dataStream = liveAPIClient.GetStream())
            {
                // Then let's begin ingesting the ProtoBuf events, and for now we'll simply return them to console
                while (true)
                {
                    while (!dataStream.DataAvailable) ;

                    byte[] bytes = new byte[liveAPIClient.Available];

                    dataStream.Read(bytes, 0, bytes.Length);

                    String data = Encoding.UTF8.GetString(bytes);

                    if (new System.Text.RegularExpressions.Regex("^GET").IsMatch(data))
                    {
                        Console.WriteLine("Another");

                        const string eol = "\r\n"; // HTTP/1.1 defines the sequence CR LF as the end-of-line marker

                        byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + eol
                            + "Connection: Upgrade" + eol
                            + "Upgrade: websocket" + eol
                            + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                                System.Security.Cryptography.SHA1.Create().ComputeHash(
                                    Encoding.UTF8.GetBytes(
                                        new System.Text.RegularExpressions.Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim()
                                    )
                                )
                            ) + eol
                        + eol);

                        dataStream.Write(response, 0, response.Length);
                    }
                    else
                    {
                        Console.WriteLine("Something");
                        Console.WriteLine(data);
                    }
                }
            }
        }

        /// <summary>
        /// This method establishes a connection to the Apex Legends LiveAPI and returns a TcpClient that can be used to recieve the 
        /// proto buf calls.
        /// </summary>
        /// <param name="ipAddress">String containing IPv4 IP address</param>
        /// <param name="port">Int containing the port to listen into</param>
        /// <returns></returns>
        static public TcpClient CreateAPIConnection(string ipAddress, int port)
        {
            using TcpListener liveAPIConnection = new TcpListener(IPAddress.Parse(ipAddress), port);
            liveAPIConnection.Start();

            Console.WriteLine($"Opened WebSocket server on IP address: {ipAddress} and port: {port}");

            using TcpClient tcpClient = liveAPIConnection.AcceptTcpClient();

            Console.WriteLine("Client connected");
            return tcpClient;
        }
    }
}