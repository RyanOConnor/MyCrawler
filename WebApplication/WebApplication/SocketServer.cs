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
    public class SocketServer
    {
        public SocketHandle receiveSocket { get; set; }
        private string ipAddress { get; set; }
        protected ManualResetEvent listenerSignal = new ManualResetEvent(false);
        protected ManualResetEvent connectDone = new ManualResetEvent(false);
        protected ManualResetEvent sendDone = new ManualResetEvent(false);
        protected ManualResetEvent receiveDone = new ManualResetEvent(false);
        public event EventHandler<MessageEventArgs> MessageReceived;

        public void StartListener(string ipAd)
        {
            ipAddress = ipAd;
            IPAddress ip = IPAddress.Parse(ipAddress);
            IPEndPoint endPoint = new IPEndPoint(ip, 11000);

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

                receiveSocket = new SocketHandle(handler);
                receiveSocket.socket.BeginReceive(receiveSocket.buffer, 0, SocketHandle.BUFFER_SIZE, 0,
                                                    new AsyncCallback(ReceiveCallBack), receiveSocket);
            }
            catch(SocketException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        protected void Receive()
        {
            try
            {
                Socket server = receiveSocket.socket;
                receiveSocket = new SocketHandle(server);
                receiveSocket.socket.BeginReceive(receiveSocket.buffer, 0, SocketHandle.BUFFER_SIZE, 0,
                                                    new AsyncCallback(ReceiveCallBack), receiveSocket);
            }
            catch(SocketException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void ReceiveCallBack(IAsyncResult result)
        {
            SocketHandle socketHandle = (SocketHandle)result.AsyncState;
            Socket server = socketHandle.socket;
            int bytesRead = socketHandle.socket.EndReceive(result);

            try
            {
                if (bytesRead > 0)
                {
                    socketHandle.str.Append(Encoding.UTF8.GetString(socketHandle.buffer, 0, bytesRead));

                    if (server.Available > 0)
                    {
                        server.BeginReceive(socketHandle.buffer, 0, SocketHandle.BUFFER_SIZE, 0,
                                                new AsyncCallback(ReceiveCallBack), socketHandle);
                    }
                    else
                    {
                        if (socketHandle.str.Length > 1)
                        {
                            string message = socketHandle.str.ToString();
                            socketHandle.Reset();
                            
                            //Console.WriteLine("\nRecieved: " + message);

                            EventHandler<MessageEventArgs> messageReceived = MessageReceived;
                            if (messageReceived != null)
                            {
                                messageReceived(server, new MessageEventArgs(message));
                            }
                            
                            Receive();
                        }
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

    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public MessageEventArgs(string message)
        {
            this.Message = message;
        }
    }
}
