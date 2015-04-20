using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using MongoDB.Bson;

namespace WebApplication
{
    public class ClientListener
    {
        protected ManualResetEvent listenerSignal = new ManualResetEvent(false);
        protected ManualResetEvent connectDone = new ManualResetEvent(false);
        protected ManualResetEvent sendDone = new ManualResetEvent(false);
        protected ManualResetEvent receiveDone = new ManualResetEvent(false);
        public event EventHandler<AndroidMessageEventArgs> MessageReceived;

        public void StartListener()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 11002);
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
                    Console.WriteLine("Connected to client: " + listener.LocalEndPoint.ToString());
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
                SocketHandle clientSocket = new SocketHandle(handler);

                clientSocket.socket.BeginReceive(clientSocket.buffer, 0, SocketHandle.BUFFER_SIZE, 0,
                                                    new AsyncCallback(ReceiveCallBack), clientSocket);
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
            Socket client = socketHandle.socket;
            int bytesRead = socketHandle.socket.EndReceive(result);

            try
            {
                if (bytesRead > 0)
                {
                    socketHandle.AppendBytes(bytesRead);

                    if (client.Available > 0)
                    {
                        client.BeginReceive(socketHandle.buffer, 0, SocketHandle.BUFFER_SIZE, 0,
                                                new AsyncCallback(ReceiveCallBack), socketHandle);
                    }
                    else
                    {
                        if (socketHandle.fullBuffer.Length > 1)
                        {
                            EventHandler<AndroidMessageEventArgs> messageReceived = MessageReceived;
                            if (messageReceived != null)
                            {
                                messageReceived.BeginInvoke(socketHandle,
                                                            new AndroidMessageEventArgs(socketHandle.fullBuffer, socketHandle.EndOfMessage()),
                                                            OnEventFinished, null);
                            }

                            socketHandle.Reset();
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        private void OnEventFinished(IAsyncResult result)
        {
            var async = (System.Runtime.Remoting.Messaging.AsyncResult)result;
            var invokedMethod = (EventHandler<MessageEventArgs>)async.AsyncDelegate;
            invokedMethod.EndInvoke(result);
        }

        protected void Send(SocketHandle clientHandle, byte[] data)
        {
            byte[] beginning = Encoding.UTF8.GetBytes("<BOF>");
            byte[] ending = Encoding.UTF8.GetBytes("<EOF>");
            byte[] message = new byte[beginning.Length + data.Length + ending.Length];
            Buffer.BlockCopy(beginning, 0, message, 0, beginning.Length);
            Buffer.BlockCopy(data, 0, message, beginning.Length, data.Length);
            Buffer.BlockCopy(ending, 0, message, beginning.Length + data.Length, ending.Length);

            if (IsConnected(clientHandle))
            {
                clientHandle.socket.BeginSend(message, 0, message.Length, 0, new AsyncCallback(SendCallBack), clientHandle);
            }
            else
            {
                //this.sendSocket.Shutdown(SocketShutdown.Both);
                //this.sendSocket.Close();
                //this.StartClient(ipAddress);

                Send(clientHandle, data);
                throw new Exception();
            }
        }

        private void SendCallBack(IAsyncResult result)
        {
            SocketHandle clientHandle = (SocketHandle)result.AsyncState;
            try
            {
                clientHandle.socket.EndSend(result);

                // TODO: Event ?

                clientHandle.socket.Shutdown(SocketShutdown.Both);
                clientHandle.socket.Close();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        private bool IsConnected(SocketHandle clientHandle)
        {
            return !(clientHandle.socket.Poll(1000, SelectMode.SelectRead) && clientHandle.socket.Available == 0);
        }
    }

}
