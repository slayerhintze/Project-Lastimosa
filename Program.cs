using System.Net;
using System.Net.Sockets;
using System.Text;
using Rtech.Liveapi;

namespace Project_Lastimosa
{
    public class Program
    {
        static void Main(string[] args)
        {
            Repl();
        }

        /// <summary>
        /// This method establishes a connection to the Apex Legends LiveAPI and returns a TcpClient that can be used to recieve the 
        /// proto buf calls.
        /// </summary>
        /// <param name="ipAddress">String containing IPv4 IP address</param>
        /// <param name="port">Int containing the port to listen into</param>
        /// <returns></returns>
        async static public void Repl()
        {
            // First let's do some testing to get the WebSocket server up and running
            // These two constants are values outlined in the README.md for this project, and as such should not be tampered with.
            string LIVE_API_IP_ADDRESS = "127.0.0.1";
            int LIVE_API_PORT = 7777;

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
                    while (liveAPIClient.Available < 3) ;

                    byte[] bytes = new byte[liveAPIClient.Available];

                    dataStream.Read(bytes, 0, bytes.Length);

                    String data = Encoding.UTF8.GetString(bytes);
                    //Console.WriteLine($"This is something {data}");

                    if (new System.Text.RegularExpressions.Regex("^GET").IsMatch(data))
                    {
                        Console.WriteLine("====Client handshake====");
                        Console.WriteLine(data);
                        Console.WriteLine();

                        const string eol = "\r\n"; // HTTP/1.1 defines the sequence CR LF as the end-of-line marker

                        byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + eol
                            + "Connection: Upgrade" + eol
                            + "Upgrade: websocket" + eol
                            + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                                System.Security.Cryptography.SHA1.Create().ComputeHash(
                                    Encoding.UTF8.GetBytes(
                                        new System.Text.RegularExpressions.Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                                    )
                                )
                            ) + eol
                        + eol);

                        dataStream.Write(response, 0, response.Length);
                    }
                    else
                    {
                        Console.WriteLine("Line");
                        bool fin = (bytes[0] & 0b10000000) != 0,
                        mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"
                        int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                            offset = 2;
                        ulong msglen = bytes[1] & (ulong)0b01111111;

                        if (msglen == 126)
                        {
                            // bytes are reversed because websocket will print them in Big-Endian, whereas
                            // BitConverter will want them arranged in little-endian on windows
                            msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                            offset = 4;
                        }
                        else if (msglen == 127)
                        {
                            // To test the below code, we need to manually buffer larger messages — since the NIC's autobuffering
                            // may be too latency-friendly for this code to run (that is, we may have only some of the bytes in this
                            // websocket frame available through client.Available).
                            msglen = BitConverter.ToUInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
                            offset = 10;
                        }

                        if (msglen == 0)
                        {
                            Console.WriteLine("msglen == 0");
                        }
                        else if (mask)
                        {
                            byte[] decoded = new byte[msglen];
                            byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                            offset += 4;

                            for (ulong i = 0; i < msglen; ++i)
                                decoded[i] = (byte)(bytes[(ulong)offset + i] ^ masks[i % 4]);

                            LiveAPIEvent msg;
                            try
                            {
                                msg = LiveAPIEvent.Parser.ParseFrom(decoded);
                                if (msg.GameMessage.Is(Init.Descriptor))
                                {
                                    var msgResult = msg.GameMessage.Unpack<Init>();
                                    Console.WriteLine(msgResult.ToString());
                                }
                                else
                                {
                                    Console.WriteLine(msg.ToString());
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Womp");

                            }
                        }
                        else
                            Console.WriteLine("mask bit not set");

                        Console.WriteLine();
                    }
                }
            }
        }
    }
}