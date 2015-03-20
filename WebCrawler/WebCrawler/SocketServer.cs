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

namespace WebCrawler
{
    public class BufferHandle
    {
        public Socket socket { get; set; }
        public byte[] buffer { get; set; }
        public const int BUFFER_SIZE = 1024;
        private StringBuilder strBuilder;
        public StringBuilder str 
        { 
            get
            {
                lock (this.strBuilder)
                {
                    return this.strBuilder;
                }
            }
            set
            {
                lock (this.strBuilder)
                {
                    this.strBuilder = value;
                }
            }
        }

        public BufferHandle()
        {
            buffer = new byte[BUFFER_SIZE];
            strBuilder = new StringBuilder();
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
        private Socket Server { get; set; }
        private ManualResetEvent ConnectDone = new ManualResetEvent(false);
        public event EventHandler Connected;
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler MessageSubmitted;
        private static SocketServer _instance;
        public static SocketServer Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SocketServer();
                return _instance;
            }
        }

        public void StartClient()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = IPAddress.Parse("192.168.1.132");
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 11000);

            try
            {
                Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Server.BeginConnect(endPoint, new AsyncCallback(ConnectCallBack), Server);
                ConnectDone.WaitOne();

                EventHandler connectedHandler = Connected;
                if(connectedHandler != null)
                {
                    connectedHandler(null, EventArgs.Empty);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public bool IsConnected()
        {
            return Server.Poll(1000, SelectMode.SelectWrite);
        }

        private void ConnectCallBack(IAsyncResult result)
        {
            try
            {
                Socket server = (Socket)result.AsyncState;
                server.EndConnect(result);

                Console.WriteLine("Socket connected to {0}", server.RemoteEndPoint.ToString());

                ConnectDone.Set();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void Receive()
        {
            try
            {
                BufferHandle socketHandle = new BufferHandle();
                Server.BeginReceive(socketHandle.buffer, 0, BufferHandle.BUFFER_SIZE, 0,
                                                    new AsyncCallback(ReceiveCallBack), socketHandle);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void ReceiveCallBack(IAsyncResult result)
        {
            BufferHandle socketHandle = (BufferHandle)result.AsyncState;
            int bytesRead = Server.EndReceive(result);
            string content = string.Empty;

            try
            {
                if (bytesRead > 0)
                {
                    socketHandle.str.Append(Encoding.UTF8.GetString(socketHandle.buffer, 0, bytesRead));
                    content = socketHandle.str.ToString();

                    if (content.IndexOf("<EOF>") > -1)
                    {
                        string message = content.Remove(content.IndexOf("<EOF>"));

                        EventHandler<MessageEventArgs> messageReceived = MessageReceived;
                        if (messageReceived != null)
                        {
                            messageReceived(null, new MessageEventArgs(message));
                        }
                        socketHandle.Reset();
                    }
                    else
                    {
                        Server.BeginReceive(socketHandle.buffer, 0, BufferHandle.BUFFER_SIZE, 0,
                                                new AsyncCallback(ReceiveCallBack), socketHandle);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void Send(string message)
        {
            message += "<EOF>";
            byte[] byteData = Encoding.UTF8.GetBytes(message);

            if(IsConnected())
            {
                Server.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallBack), Server);
                Console.WriteLine("Sent: \n" + message);
            }
            else
            {
                SocketServer.Instance.StartClient();
                SocketServer.Instance.Send(message);
                throw new Exception();
            }
        }

        public void SendCallBack(IAsyncResult result)
        {
            try
            {
                Socket server = (Socket)result.AsyncState;
                server.EndSend(result);

                EventHandler messageSubmitted = MessageSubmitted;
                if (messageSubmitted != null)
                {
                    messageSubmitted(server, EventArgs.Empty);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
