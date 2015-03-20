using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Json;

namespace WebApplication
{
    public class SocketHandle
    {
        public Socket socket { get; set; }
        public byte[] buffer { get; set; }
        public const int BUFFER_SIZE = 1024;
        private StringBuilder strBuilder;
        public StringBuilder str 
        { 
            get
            {
                lock(this.strBuilder)
                {
                    return this.strBuilder;
                }
            }
            set
            {
                lock(this.strBuilder)
                {
                    this.strBuilder = value;
                }
            }
        }

        public ConsoleColor color { get; set; }

        public SocketHandle(Socket socket, ConsoleColor color)
        {
            this.socket = socket;
            buffer = new byte[BUFFER_SIZE];
            strBuilder = new StringBuilder();
            this.color = color;
        }

        public void Reset()
        {
            this.strBuilder = new StringBuilder();
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public MessageEventArgs(string message)
        {
            this.Message = message;
        }
    }

    public class SocketServer
    {
        private Dictionary<IPEndPoint, SocketHandle> clients = new Dictionary<IPEndPoint, SocketHandle>();
        protected ManualResetEvent listenerSignal = new ManualResetEvent(false);
        protected ManualResetEvent connectDone = new ManualResetEvent(false);
        protected ManualResetEvent sendDone = new ManualResetEvent(false);
        protected ManualResetEvent receiveDone = new ManualResetEvent(false);
        public event EventHandler<MessageEventArgs> MessageReceived;

        protected int colorInt = 0;
        protected ConsoleColor[] nodeColors = new ConsoleColor[] { ConsoleColor.Cyan, ConsoleColor.Yellow, 
                                                                        ConsoleColor.Red, ConsoleColor.Blue, 
                                                                        ConsoleColor.Green, ConsoleColor.Magenta,
                                                                        ConsoleColor.White, ConsoleColor.Gray};

        public void StartListener()
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
                    listener.BeginAccept(new AsyncCallback(ConnectCallBack), listener);

                    listenerSignal.WaitOne();

                    Console.WriteLine("Connected to crawler: " + listener.LocalEndPoint.ToString());
                    colorInt++;
                }
            }
            catch(SocketException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        protected virtual void ConnectCallBack(IAsyncResult result)
        {
            listenerSignal.Set();

            try
            {
                Socket listener = (Socket)result.AsyncState;
                Socket handler = listener.EndAccept(result);
                SocketHandle socketHandle;

                lock(this.clients)
                {
                    socketHandle = new SocketHandle(handler, nodeColors[colorInt]);
                    clients.Add((IPEndPoint)socketHandle.socket.LocalEndPoint, socketHandle);
                }

                socketHandle.socket.BeginReceive(socketHandle.buffer, 0, SocketHandle.BUFFER_SIZE, 0,
                                                    new AsyncCallback(ReceiveCallBack), socketHandle);
            }
            catch(SocketException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        protected void Recieve(SocketHandle handle)
        {
            handle.socket.BeginReceive(handle.buffer, 0, SocketHandle.BUFFER_SIZE, 0,
                                        new AsyncCallback(ReceiveCallBack), handle);
        }

        protected void ReceiveCallBack(IAsyncResult result)
        {
            SocketHandle socketHandle = (SocketHandle)result.AsyncState;
            int bytesRead = socketHandle.socket.EndReceive(result);
            string content = string.Empty;

            try
            {
                if (bytesRead > 0)
                {
                    socketHandle.str.Append(Encoding.UTF8.GetString(socketHandle.buffer, 0, bytesRead));
                    content = socketHandle.str.ToString();

                    if (content.EndsWith("<EOF>"))
                    {
                        string message = content.Remove(content.IndexOf("<EOF>"));
                        Console.WriteLine("\nRecieved: " + message);

                        EventHandler<MessageEventArgs> messageReceived = this.MessageReceived;
                        if (messageReceived != null)
                        {
                            messageReceived(socketHandle, new MessageEventArgs(message));
                        }
                        socketHandle.Reset();
                    }
                    else
                    {
                        socketHandle.socket.BeginReceive(socketHandle.buffer, 0, SocketHandle.BUFFER_SIZE, 0,
                                        new AsyncCallback(ReceiveCallBack), socketHandle);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public bool IsConnected(SocketHandle handle)
        {
            return handle.socket.Poll(1000, SelectMode.SelectWrite);
        }

        public void Send(SocketHandle handle, string data)
        {
            data += "<EOF>";
            byte[] byteData = Encoding.UTF8.GetBytes(data);

            if (IsConnected(handle))
            {
                handle.socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallBack), handle);
                Console.WriteLine(this.ToString() + " Sent: \n" + data);
            }
            else
            {
                throw new Exception("Couldn't Connect to node/client");
            }
        }

        private void SendCallBack(IAsyncResult result)
        {
            try
            {
                SocketHandle socketHandle = (SocketHandle)result.AsyncState;

                int bytesSent = socketHandle.socket.EndSend(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public string SerializeToJSON(object obj, Type type)
        {
            try
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(type);
                MemoryStream stream = new MemoryStream();
                ser.WriteObject(stream, obj);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public object DeserializeJSON(string message, Type type)
        {
            try
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(type);
                MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(message));
                return ser.ReadObject(stream) as object;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }
    }
}
