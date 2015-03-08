using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Collections.Concurrent;

namespace WebCrawler
{
    public class SocketHandle
    {
        public Socket socket { get; set; }
        public byte[] buffer { get; set; }
        public const int bufferSize = 1024;
        public StringBuilder sb { get; set; }

        public SocketHandle(Socket clientSocket)
        {
            buffer = new byte[bufferSize];
            socket = clientSocket;
            sb = new StringBuilder();
        }
    }

    public static class SocketServer
    {
        private static BlockingCollection<HTMLPage> sendQueue { get; set; }
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        public static void startClient()
        {
            sendQueue = new BlockingCollection<HTMLPage>();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = IPAddress.Parse("192.168.1.132");
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 11000);

            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                server.BeginConnect(endPoint, new AsyncCallback(connectCallBack), server);
                connectDone.WaitOne();

                Thread communicate = new Thread(() => communicateLoop(server));
                communicate.Start();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void reconnectToServer()
        {
            IPAddress ipAddress = IPAddress.Parse("192.168.1.132");
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 11000);

            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                server.BeginConnect(endPoint, new AsyncCallback(connectCallBack), server);
                connectDone.WaitOne();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void communicateLoop(Socket serverSocket)
        {
            SocketHandle server = new SocketHandle(serverSocket);

            try
            {
                while (true)
                {
                    string message = string.Empty;        // JSON OBJECT ON SENDQUEUE.DEQUEUE
                    if(sendQueue.Count != 0)
                    {
                        message = serializeToJSON(sendQueue.Take());
                        if(!server.socket.Poll(1, SelectMode.SelectWrite))
                        {
                            reconnectToServer();
                        }
                    }
                    else
                    {
                        message = "next";
                    }

                    clientSend(server, message + "<EOF>");
                    sendDone.WaitOne();

                    receive(server);
                    receiveDone.WaitOne();

                    server.sb.Clear();
                    receiveDone.Reset();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void connectCallBack(IAsyncResult result)
        {
            try
            {
                Socket server = (Socket)result.AsyncState;
                server.EndConnect(result);

                Console.WriteLine("Socket connected to {0}", server.RemoteEndPoint.ToString());

                connectDone.Set();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void clientSend(SocketHandle serverSocket, string data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            serverSocket.socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(clientSendCallBack), serverSocket);
        }

        private static void clientSendCallBack(IAsyncResult result)
        {
            try
            {
                SocketHandle serverSocket = (SocketHandle)result.AsyncState;
                int bytesSent = serverSocket.socket.EndSend(result);
                sendDone.Set();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void receive(SocketHandle serverHandle)
        {
            try
            {
                serverHandle.socket.BeginReceive(serverHandle.buffer, 0, SocketHandle.bufferSize, 0, 
                                                    new AsyncCallback(receiveCallBack), serverHandle);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void receiveCallBack(IAsyncResult result)
        {
            string content = string.Empty;
            SocketHandle socketHandle = (SocketHandle)result.AsyncState;

            int bytesRead = socketHandle.socket.EndReceive(result);

            try
            {
                if(bytesRead > 0)
                {
                    socketHandle.sb.Append(Encoding.ASCII.GetString(socketHandle.buffer, 0, bytesRead));
                    content = socketHandle.sb.ToString();

                    if(content.IndexOf("<EOF>") > -1)
                    {
                        if (content.StartsWith("next"))
                        {
                            // UNBLOCK MAIN THREAD SO IT CAN SEND 
                            
                        }
                        else
                        {
                            // PLACE JSON OBJECT INTO WORK QUEUE
                            WebCrawler.enqueueWorkQueue(deserializeJSON(content));
                        }
                        receiveDone.Set();
                    }
                    else
                    {
                        socketHandle.socket.BeginReceive(socketHandle.buffer, 0, SocketHandle.bufferSize, 0,
                                                            new AsyncCallback(receiveCallBack), socketHandle);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static HTMLPage deserializeJSON(string message)
        {
            if (message.EndsWith("<EOF>"))
            {
                string token = "<EOF>";
                char[] eof = token.ToCharArray();
                message = message.TrimEnd(eof);
            }
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

        public static void sendHTMLPage(HTMLPage page)
        {
            sendQueue.Add(page);
            receiveDone.Set();
            Console.WriteLine("Trying to send {0}...", page.domain.AbsoluteUri);
            Console.ReadLine();
        }
    }
}
