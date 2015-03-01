using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WebApplication
{
    public class SocketHandle
    {
        public Socket socket { get; set; }
        public byte[] buffer { get; set; }
        public const int bufferSize = 1024;
        public StringBuilder sb { get; set; }

        public ConsoleColor color { get; set; }
        public int consoleColorInt { get; set; }

        public SocketHandle(Socket clientSocket, ConsoleColor color, int colorInt)
        {
            buffer = new byte[bufferSize];
            socket = clientSocket;
            sb = new StringBuilder();
            this.color = color;
            this.consoleColorInt = colorInt;
        }
    }

    public class SocketServer
    {
        private static ManualResetEvent listenerSignal = new ManualResetEvent(false);
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);
        private static string response = string.Empty;

        private static int colorInt = 0;
        private static ConsoleColor[] colors = new ConsoleColor[] { ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.Blue, ConsoleColor.Green };

        public static void startListener()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[3];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 11000);

            Console.WriteLine("Local address and port: {0}", endPoint.ToString());

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(endPoint);
                listener.Listen(100);

                while(true)
                {
                    listenerSignal.Reset();

                    Console.WriteLine("Waiting for connection...");
                    listener.BeginAccept(new AsyncCallback(acceptCallBack), listener);

                    listenerSignal.WaitOne();

                    Console.WriteLine("Connected to crawler");
                    colorInt++;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("Closing the listener. Hit enter to continue...");
            Console.Read();
        }

        public static void acceptCallBack(IAsyncResult result)
        {
            // Signal main thread to continue
            listenerSignal.Set();

            Socket listener = (Socket)result.AsyncState;
            Socket handler = listener.EndAccept(result);

            SocketHandle socketHandle = new SocketHandle(handler, colors[colorInt], colorInt);
            handler.BeginReceive(socketHandle.buffer, 0, SocketHandle.bufferSize, 0, 
                                    new AsyncCallback(readCallBack), socketHandle);
        }

        public static void readCallBack(IAsyncResult result)
        {
            string content = string.Empty;

            SocketHandle socketHandle = (SocketHandle)result.AsyncState;
            Socket handler = socketHandle.socket;

            int bytesRead = handler.EndReceive(result);

            if (bytesRead > 0)
            {
                string temp = Encoding.ASCII.GetString(socketHandle.buffer, 0, bytesRead); // check string going in
                socketHandle.sb.Append(Encoding.ASCII.GetString(socketHandle.buffer, 0, bytesRead));

                content = socketHandle.sb.ToString();

                if (content.Length > 15)            // CATCH BUFFER OVERFLOW
                    Console.WriteLine();            // determine way to queue buffers quickly? each node is a thread and holds a recieveQueue for processing.... ?

                if (content.IndexOf("<EOF>") > -1)
                {
                    //Console.WriteLine("Read {0} bytes from socket.", content.Length);
                    Console.ForegroundColor = socketHandle.color;
                    Console.WriteLine("\tData: {0}", content);
                    socketHandle.sb.Clear();
                    listenerSend(socketHandle, "next<EOF>");
                }
                else
                {
                    handler.BeginReceive(socketHandle.buffer, 0, SocketHandle.bufferSize, 0, 
                                            new AsyncCallback(readCallBack), socketHandle);
                }
            }
        }

        private static void listenerSend(SocketHandle handler, string data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            handler.socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(listenerSendCallBack), handler);
        }

        private static void listenerSendCallBack(IAsyncResult result)
        {
            try
            {
                //Socket handler = (Socket)result.AsyncState;
                SocketHandle socketHandle = (SocketHandle)result.AsyncState;

                int bytesSent = socketHandle.socket.EndSend(result);
                //Console.WriteLine("Sent {0} bytes to server.", bytesSent);
                
                //handler.Shutdown(SocketShutdown.Both);      // Determine way to maintain connection and remove this
                //handler.Close();
                socketHandle.socket.BeginReceive(socketHandle.buffer, 0, SocketHandle.bufferSize, 0,
                                    new AsyncCallback(readCallBack), socketHandle);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /*public static void startClient()
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                IPEndPoint endPoint = new IPEndPoint(ipAddress, 8888);

                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                
                client.BeginConnect(endPoint, new AsyncCallback(connectCallBack), client);

                connectDone.WaitOne();

                clientSend(client, "This is a test<EOF>"); 
                sendDone.WaitOne();

                receive(client);
                receiveDone.WaitOne();

                Console.WriteLine("Response received: {0}", response);

                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void connectCallBack(IAsyncResult result)
        {
            try
            {
                Socket client = (Socket)result.AsyncState;
                client.EndConnect(result);

                Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());

                connectDone.Set();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void receive(Socket client)
        {
            try
            {
                SocketHandle socketHandle = new SocketHandle(client);

                client.BeginReceive(socketHandle.buffer, 0, SocketHandle.bufferSize, 0, 
                                        new AsyncCallback(receiveCallBack), socketHandle);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void receiveCallBack(IAsyncResult result)
        {
            try
            {
                SocketHandle socketHandle = (SocketHandle)result.AsyncState;
                Socket client = socketHandle.socket;

                int bytesRead = client.EndReceive(result);

                if (bytesRead > 0)
                {
                    socketHandle.sb.Append(Encoding.ASCII.GetString(socketHandle.buffer, 0, bytesRead));

                    client.BeginReceive(socketHandle.buffer, 0, SocketHandle.bufferSize, 0, 
                                            new AsyncCallback(receiveCallBack), socketHandle);
                }
                else
                {
                    if (socketHandle.sb.Length > 1)
                    {
                        response = socketHandle.sb.ToString();
                    }
                    receiveDone.Set();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void clientSend(Socket handler, string data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(clientSendCallBack), handler);
        }

        private static void clientSendCallBack(IAsyncResult result)
        {
            try
            {
                Socket handler = (Socket)result.AsyncState;

                int bytesSent = handler.EndSend(result);
                Console.WriteLine("Sent {0} bytes to client", bytesSent);

                sendDone.Set();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }*/
    }
}
