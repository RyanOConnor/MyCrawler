﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WebCrawler
{
    class SocketServer
    {
        private SocketHandle receiveSocket { get; set; }
        private string ipAddress { get; set; }
        public event EventHandler ServerConnected;
        public event EventHandler<MessageEventArgs> MessageReceived;
        protected ManualResetEvent listenerSignal = new ManualResetEvent(false);
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

        public void StartListener(string ipAd)
        {
            ipAddress = ipAd;
            IPAddress ip = IPAddress.Parse(ipAddress);
            IPEndPoint endPoint = new IPEndPoint(ip, 11001);

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(endPoint);
                listener.Listen(100);

                while (true)
                {
                    listenerSignal.Reset();

                    listener.BeginAccept(new AsyncCallback(ConnectCallBack), listener);

                    listenerSignal.WaitOne();

                    EventHandler connectedHandler = ServerConnected;
                    if(connectedHandler != null)
                    {
                        connectedHandler(null, EventArgs.Empty);
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void ConnectCallBack(IAsyncResult result)
        {
            listenerSignal.Set();

            try
            {
                Socket listener = (Socket)result.AsyncState;
                Socket handler = listener.EndAccept(result);

                Console.WriteLine("SocketClient connected to {0}", handler.RemoteEndPoint.ToString());

                receiveSocket = new SocketHandle(handler);
                receiveSocket.socket.BeginReceive(receiveSocket.buffer, 0, SocketHandle.BUFFER_SIZE, 0,
                                                    new AsyncCallback(ReceiveCallBack), receiveSocket);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void Receive()
        {
            try
            {
                Socket server = receiveSocket.socket;
                receiveSocket = new SocketHandle(server);
                receiveSocket.socket.BeginReceive(receiveSocket.buffer, 0, SocketHandle.BUFFER_SIZE, 0,
                                                    new AsyncCallback(ReceiveCallBack), receiveSocket);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void ReceiveCallBack(IAsyncResult result)
        {
            SocketHandle socketHandle = (SocketHandle)result.AsyncState;
            Socket client = socketHandle.socket;
            int bytesRead = client.EndReceive(result);

            try
            { 
                if (bytesRead > 0)
                {
                    socketHandle.str.Append(Encoding.UTF8.GetString(socketHandle.buffer, 0, bytesRead));

                    if (client.Available > 0)
                    {
                        client.BeginReceive(socketHandle.buffer, 0, SocketHandle.BUFFER_SIZE, 0,
                                                            new AsyncCallback(ReceiveCallBack), socketHandle);
                    }
                    else
                    {
                        if (socketHandle.str.Length > 1)
                        {
                            string response = socketHandle.str.ToString();
                            socketHandle.Reset();

                            EventHandler<MessageEventArgs> messageReceived = MessageReceived;
                            if (messageReceived != null)
                            {
                                messageReceived(client, new MessageEventArgs(response));
                            }

                            Receive();    
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
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
