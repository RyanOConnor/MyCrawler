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
    public class AsyncListener
    {
        public SocketHandle receiveSocket { get; set; }
        private IPAddress ipAddress { get; set; }
        protected ManualResetEvent listenerSignal = new ManualResetEvent(false);
        protected ManualResetEvent connectDone = new ManualResetEvent(false);
        protected ManualResetEvent sendDone = new ManualResetEvent(false);
        protected ManualResetEvent receiveDone = new ManualResetEvent(false);
        public event EventHandler<MessageEventArgs> MessageReceived;


        public void StartListener(IPAddress ipAdd)
        {
            ipAddress = ipAdd;
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 11000);

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
                    listener.BeginAccept(new AsyncCallback(ConnectCallBack), listener);

                    listenerSignal.WaitOne();
                    Console.WriteLine("Connected to crawler: " + listener.LocalEndPoint.ToString());
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
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
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        protected void Receive(IAsyncResult result)
        {
            try
            {
                if (result != null)
                {
                    var async = (System.Runtime.Remoting.Messaging.AsyncResult)result;
                    var invokedMethod = (EventHandler<MessageEventArgs>)async.AsyncDelegate;
                    invokedMethod.EndInvoke(result);
                }

                Socket server = receiveSocket.socket;
                receiveSocket = new SocketHandle(server);
                receiveSocket.socket.BeginReceive(receiveSocket.buffer, 0, SocketHandle.BUFFER_SIZE, 0,
                                                    new AsyncCallback(ReceiveCallBack), receiveSocket);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
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
                    //socketHandle.byteStream.Write(socketHandle.buffer, 0, bytesRead);

                    if (server.Available > 0)
                    {
                        server.BeginReceive(socketHandle.buffer, 0, SocketHandle.BUFFER_SIZE, 0,
                                                new AsyncCallback(ReceiveCallBack), socketHandle);
                    }
                    else
                    {
                        //if (socketHandle.byteStream.Length > 1)
                        {
                            EventHandler<MessageEventArgs> messageReceived = MessageReceived;
                            if (messageReceived != null)
                            {
                                //messageReceived.Invoke(null, new MessageEventArgs(socketHandle.byteStream.ToArray()));
                               // messageReceived.BeginInvoke(null, new MessageEventArgs(socketHandle.byteStream.ToArray(), socketHandle.endOfMessage), Receive, null);
                            }

                            socketHandle.Reset();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }
    }
}
