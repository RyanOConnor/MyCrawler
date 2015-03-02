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
        public StringBuilder strBuilder { get; set; }

        public ConsoleColor color { get; set; }
        public int consoleColorInt { get; set; }

        public SocketHandle(Socket clientSocket, ConsoleColor color, int colorInt)
        {
            buffer = new byte[bufferSize];
            socket = clientSocket;
            strBuilder = new StringBuilder();
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
        private static ConsoleColor[] nodeColors = new ConsoleColor[] { ConsoleColor.Cyan, ConsoleColor.Yellow, 
                                                                    ConsoleColor.Red, ConsoleColor.Blue, 
                                                                    ConsoleColor.Green, ConsoleColor.Magenta,
                                                                    ConsoleColor.White, ConsoleColor.Gray};

        public static void startListener()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = IPAddress.Parse("192.168.1.132");
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

                    Console.WriteLine("Connected to crawler: " + listener.LocalEndPoint.ToString());
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
            listenerSignal.Set();

            Socket listener = (Socket)result.AsyncState;
            Socket handler = listener.EndAccept(result);

            SocketHandle socketHandle = new SocketHandle(handler, nodeColors[colorInt], colorInt);
            handler.BeginReceive(socketHandle.buffer, 0, SocketHandle.bufferSize, 0, 
                                    new AsyncCallback(readCallBack), socketHandle);
        }

        public static void readCallBack(IAsyncResult result)
        {
            string content = string.Empty;
            SocketHandle socketHandle = (SocketHandle)result.AsyncState;

            int bytesRead = socketHandle.socket.EndReceive(result);

            try
            {
                if (bytesRead > 0)
                {
                    socketHandle.strBuilder.Append(Encoding.ASCII.GetString(socketHandle.buffer, 0, bytesRead));
                    content = socketHandle.strBuilder.ToString();

                    if (content.IndexOf("<EOF>") > -1)
                    {
                        Console.WriteLine("Read {0} bytes from socket.", content.Length);
                        Console.ForegroundColor = socketHandle.color;
                        Console.WriteLine("\tData: {0}", content);

                        if (content.StartsWith("next"))
                        {
                            // SEND NEXT WORK QUEUE ITEM
                            string JSON = CrawlerManager.sendWorkToCrawler();
                            listenerSend(socketHandle, JSON + "<EOF>");
                            Console.WriteLine("Sent: " + JSON);
                        }
                        else
                        {
                            // RELAY JSON STRING TO DATABASE
                            string token = "<EOF>";
                            char[] eof = token.ToCharArray();
                            content = content.TrimEnd(eof);
                            CrawlerManager.relayCrawlerResults(CrawlerManager.deserializeJSON(content));
                            listenerSend(socketHandle, "next<EOF>");
                        }

                        socketHandle.strBuilder.Clear();
                    }
                    else
                    {
                        socketHandle.socket.BeginReceive(socketHandle.buffer, 0, SocketHandle.bufferSize, 0,
                                                            new AsyncCallback(readCallBack), socketHandle);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
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
    }
}
