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
    public class SocketClient
    {
        public Socket sendSocket { get; set; }
        private string ipAddress = string.Empty;
        private ManualResetEvent connectDone = new ManualResetEvent(false);
        public event EventHandler<MessageEventArgs> ClientConnected;
        public event EventHandler MessageSubmitted;
        private static SocketClient _instance;
        public static SocketClient Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SocketClient();
                return _instance;
            }
        }

        public void StartClient(string ipAd)
        {
            ipAddress = ipAd;
            IPAddress ip = IPAddress.Parse(ipAd);
            IPEndPoint endPoint = new IPEndPoint(ip, 11000);

            sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            try
            {
                sendSocket.BeginConnect(endPoint, new AsyncCallback(ConnectCallBack), sendSocket);
                connectDone.WaitOne();

                EventHandler<MessageEventArgs> connectedHandler = ClientConnected;
                if(connectedHandler != null)
                {
                    connectedHandler(null, new MessageEventArgs(ipAddress));
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void ConnectCallBack(IAsyncResult result)
        {
            try
            {
                Socket senderSocket = (Socket)result.AsyncState;
                senderSocket.EndConnect(result);

                Console.WriteLine("SocketClient connected to {0}", senderSocket.RemoteEndPoint.ToString());

                connectDone.Set();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public bool IsConnected()
        {
            return sendSocket.Poll(1000, SelectMode.SelectWrite);
        }

        public void Send(string message)
        {
            byte[] byteData = Encoding.UTF8.GetBytes(message);

            if(IsConnected())
            {
                sendSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallBack), sendSocket);
                //Console.WriteLine("Sent: \n" + message);
            }
            else
            {
                SocketClient.Instance.StartClient(ipAddress);
                SocketClient.Instance.Send(message);
                throw new Exception();
            }
        }

        public void SendCallBack(IAsyncResult result)
        {
            try
            {
                Socket sendingSocket = (Socket)result.AsyncState;
                sendingSocket.EndSend(result);

                EventHandler messageSubmitted = MessageSubmitted;
                if (messageSubmitted != null)
                {
                    messageSubmitted(null, EventArgs.Empty);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

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

        public SocketHandle(Socket socket)
        {
            this.socket = socket;
            buffer = new byte[BUFFER_SIZE];
            strBuilder = new StringBuilder();
        }

        public void Reset()
        {
            this.strBuilder.Clear();
        }
    }

}
