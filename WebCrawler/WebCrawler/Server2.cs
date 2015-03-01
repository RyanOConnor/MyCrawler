using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.IO;

namespace WebCrawler
{
    static class Server3
    {
        private static Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static NetworkStream stream { get; set; }
        private static byte[] buffer2 = new byte[1024];

        /*public static void SendLoop()
        {
            while (true)
            {
                byte[] receivedBuffer = new byte[clientSocket.ReceiveBufferSize];
                NetworkStream stream = new NetworkStream(clientSocket);
                stream.Read(receivedBuffer, 0, receivedBuffer.Length);

                byte[] data = new byte[received];
                Array.Copy(receivedBuffer, data, received);
                Console.WriteLine("Received: " + Encoding.ASCII.GetString(data));


                Console.Write("Responding to server...");
                // DETERMINE MESSAGE TO SEND HERE
                string message = determineResponse();

                byte[] buffer = Encoding.ASCII.GetBytes(message);
                clientSocket.Send(buffer);
            }
        }*/

        public static void LoopConnect()
        {
            int attempts = 0;

            while (!clientSocket.Connected)
            {
                try
                {
                    attempts++;

                    clientSocket.Connect(IPAddress.Loopback, 100);
                }
                catch (SocketException)
                {
                    Console.Clear();
                    Console.WriteLine("Connection attempts: " + attempts.ToString());
                }
            }

            Console.Clear();
            Console.WriteLine("connected");
            string initialMessage = "next";
            byte[] initialBuffer = Encoding.ASCII.GetBytes(initialMessage);
            clientSocket.Send(initialBuffer);
            waitForData(null);
        }

        public static void waitForData(IAsyncResult result)
        {
            byte[] receivedBuffer = new byte[clientSocket.ReceiveBufferSize];
            stream = new NetworkStream(clientSocket);
            
            stream.BeginRead(receivedBuffer, 0, receivedBuffer.Length, messageCallBack, receivedBuffer);
        }

        public static void messageCallBack(IAsyncResult result)
        {
            //Socket socket = (Socket)result.AsyncState;

            int received = stream.EndRead(result);
            byte[] dataBuffer = result.AsyncState as byte[];

            string receivedMessage = Encoding.ASCII.GetString(dataBuffer);
            Console.WriteLine("Received: " + receivedMessage);

            string response = string.Empty;

            if(receivedMessage == "status")
            {
                response = "";
            }
            else
            {
                response = determineResponse();
            }

            Console.Write("Responding to server...");

            byte[] sendBytes = Encoding.ASCII.GetBytes(response);
            stream.Write(sendBytes, 0, sendBytes.Length);
            stream.Flush();
            //clientSocket.BeginSend(sendBytes, 0, sendBytes.Length, SocketFlags.None, new AsyncCallback(sendCallBack), clientSocket);
            Console.WriteLine("Message Sent: " + response);
            waitForData(null);
        }

        public static void sendCallBack(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            socket.EndSend(result);
        }

        private static string determineResponse()
        {
            if(WebCrawler.isWorkQueueEmpty())
            {
                return "next";
            }
            else
            {
                return serializeToJSON(WebCrawler.sendQueueTake());
            }
        }

        private static HTMLPage deserializeJSON(string message)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HTMLPage));
            MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(message));
            return ser.ReadObject(stream) as HTMLPage;
        }

        private static string serializeToJSON(HTMLPage page)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HTMLPage));
            MemoryStream stream = new MemoryStream();
            ser.WriteObject(stream, page);
            return Encoding.ASCII.GetString(stream.ToArray());
        }
    }

}
