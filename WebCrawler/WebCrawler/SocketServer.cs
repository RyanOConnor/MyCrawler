using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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
        private static ManualResetEvent listenerSignal = new ManualResetEvent(false);
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);
        
        /*public static void startListener()
        {
            //IPHostEntry hostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = hostInfo.AddressList[4];
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 8888);

            Console.WriteLine("Local address and port: {0}", endPoint.ToString());

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(endPoint);
                listener.Listen(100);

                while (true)
                {
                    listenerSignal.Reset();

                    Console.WriteLine("Waiting for connection...");
                    listener.BeginAccept(new AsyncCallback(acceptCallBack), listener);

                    listenerSignal.WaitOne();

                    Console.WriteLine("Connected to application");
                }
            }
            catch (Exception ex)
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

            SocketHandle socketHandle = new SocketHandle(handler);
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
                socketHandle.sb.Append(Encoding.ASCII.GetString(socketHandle.buffer, 0, bytesRead));

                content = socketHandle.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    Console.WriteLine("Read {0} bytes from socket.\n Data : {1}", content.Length, content);
                    listenerSend(handler, content);
                }
                else
                {
                    handler.BeginReceive(socketHandle.buffer, 0, SocketHandle.bufferSize, 0, 
                                            new AsyncCallback(readCallBack), socketHandle);
                }
            }
        }

        private static void listenerSend(Socket handler, string data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            handler.BeginSend(byteData, 0, byteData.Length, 0, 
                                new AsyncCallback(listenerSendCallBack), handler);
        }

        private static void listenerSendCallBack(IAsyncResult result)
        {
            try
            {
                Socket handler = (Socket)result.AsyncState;

                int bytesSent = handler.EndSend(result);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }*/

        public static void startClient()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[3];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 11000);

            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                server.BeginConnect(endPoint, new AsyncCallback(connectCallBack), server);
                connectDone.WaitOne();

                communicateLoop(server);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void communicateLoop(Socket serverSocket)
        {
            SocketHandle server = new SocketHandle(serverSocket);

            int counter = 101010101;
            try
            {
                while (true)
                {
                    string message = counter.ToString();

                    clientSend(server, message + "<EOF>");
                    sendDone.WaitOne();

                    receive(server);
                    receiveDone.WaitOne();

                    Console.WriteLine("Response received: {0}", server.sb.ToString());
                    counter++;

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
                Console.WriteLine("Sent {0} bytes to client", bytesSent);

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
            try
            {
                string content = string.Empty;
                SocketHandle socketHandle = (SocketHandle)result.AsyncState;
                //Socket client = socketHandle.socket;

                int bytesRead = socketHandle.socket.EndReceive(result);

                if(bytesRead > 0)
                {
                    // RECIEVE JSON STRING HERE
                    socketHandle.sb.Append(Encoding.ASCII.GetString(socketHandle.buffer, 0, bytesRead));
                    content = socketHandle.sb.ToString();

                    if(content.IndexOf("<EOF>") > -1)
                    {
                        //response = socketHandle.sb.ToString();
                        if (content.StartsWith("next"))
                        {
                            receiveDone.Set();
                        }
                    }
                    else
                    {
                        socketHandle.socket.BeginReceive(socketHandle.buffer, 0, SocketHandle.bufferSize, 0,
                                                            new AsyncCallback(receiveCallBack), socketHandle);
                    }
                }
                else
                {
                    if(socketHandle.sb.Length > 1)
                    {
                        // DO SOMETHING WITH MESSAGE
                        socketHandle.sb.Clear();
                        //response = socketHandle.sb.ToString();
                    }
                    
                    if(content.StartsWith("next"))
                    {
                        receiveDone.Set();
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            //receiveDone.Set();      // TODO: have this continue the main thread only after truly recieving a response
        }

    }
}
