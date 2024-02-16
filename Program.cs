using System;
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

                    StreamReader rdr = new StreamReader(dataStream);

                    while (!rdr.EndOfStream)
                    {
                        Console.WriteLine(rdr.ReadLine());
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
