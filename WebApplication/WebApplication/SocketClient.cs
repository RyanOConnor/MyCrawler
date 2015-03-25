using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace WebApplication
{
    public class SocketClient
    {
        public SocketHandle sendSocket { get; private set; }
        public string ipAddress { get; set; }
        private ManualResetEvent ConnectDone = new ManualResetEvent(false);
        public event EventHandler ClientConnected;
        public event EventHandler MessageSubmitted;

        public void StartClient(string ipAd)
        {
            ipAddress = ipAd;
            IPAddress ip = IPAddress.Parse(ipAddress);
            IPEndPoint endPoint = new IPEndPoint(ip, 11001);

            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sendSocket = new SocketHandle(client);

            try
            {
                sendSocket.socket.BeginConnect(endPoint, new AsyncCallback(ConnectCallBack), sendSocket.socket);
                ConnectDone.WaitOne();

                EventHandler connectedHandler = ClientConnected;
                if (connectedHandler != null)
                {
                    connectedHandler(null, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        private void ConnectCallBack(IAsyncResult result)
        {
            try
            {
                Socket senderSocket = (Socket)result.AsyncState;
                senderSocket.EndConnect(result);

                Console.WriteLine("Socket connected to {0}", senderSocket.RemoteEndPoint.ToString());

                ConnectDone.Set();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public bool IsConnected()
        {
            return sendSocket.socket.Poll(1000, SelectMode.SelectWrite);
        }

        public void Send(string message)
        {
            byte[] byteData = Encoding.UTF8.GetBytes(message);

            if (IsConnected())
            {
                sendSocket.socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallBack), sendSocket.socket);
                //Console.WriteLine("Sent: \n" + message);
            }
            else
            {
                this.sendSocket.socket.Shutdown(SocketShutdown.Both);
                this.sendSocket.socket.Close();

                this.StartClient(ipAddress);
                this.Send(message);
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
                    messageSubmitted(sendingSocket, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
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
