using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace WebApplication
{
    static class SocketServer
    {
        static Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static List<Socket> clientSockets = new List<Socket>();
        static byte[] buffer = new byte[1024];

        public static void startServer()
        {
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 100));
            serverSocket.Listen(1);
            serverSocket.BeginAccept(new AsyncCallback(acceptCallBack), null);
        }

        public static void acceptCallBack(IAsyncResult result)
        {
            Socket socket = serverSocket.EndAccept(result);
            clientSockets.Add(socket);
            Console.WriteLine("Client connected..");
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallBack), socket);
            serverSocket.BeginAccept(new AsyncCallback(acceptCallBack), null);

        }

        public static void receiveCallBack(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            int received = socket.EndReceive(result);
            byte[] dataBuffer = new byte[received];
            Array.Copy(buffer, dataBuffer, received);

            string message = Encoding.ASCII.GetString(dataBuffer);
            Console.WriteLine("Message received: " + message);

            string response = string.Empty;

            if(message.ToLower() != "get time")
            {
                response = "Invalid Request";
            }
            else
            {
                response = DateTime.Now.ToLongTimeString();
            }

            byte[] data = Encoding.ASCII.GetBytes(message);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(sendCallBack), socket);

            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallBack), socket);
            serverSocket.BeginAccept(new AsyncCallback(acceptCallBack), null);
        }

        private static void sendCallBack(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            socket.EndSend(result);
        }
    }

     static class TimeClient
    {
         private static Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

         private static void SendLoop()
         {
             while(true)
             {
                 Console.Write("Enter a request: ");
                 string request = Console.ReadLine();
                 byte[] buffer = Encoding.ASCII.GetBytes(request);
                 clientSocket.Send(buffer);

                 byte[] receivedBuffer = new byte[1024];
                 int received = clientSocket.Receive(receivedBuffer);
                 byte[] data = new byte[received];
                 Array.Copy(receivedBuffer, data, received);
                 Console.WriteLine("Received: " + Encoding.ASCII.GetString(data));
             }
         }

         private static void LoopConnect()
         {
             while (!clientSocket.Connected)
             {
                 int attempts = 0;
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
         }
    }
}
