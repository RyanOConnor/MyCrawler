using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MongoDB.Bson;

namespace AndroidAppServer
{
    public class SocketServer
    {
        protected SocketHandle receiveHandle { get; set; }
        private Socket sendSocket { get; set; }
        private IPAddress ipAddress { get; set; }
        protected ManualResetEvent listenerSignal = new ManualResetEvent(false);
        private ManualResetEvent ConnectDone = new ManualResetEvent(false);
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler ClientConnected;
        public event EventHandler MessageSubmitted;

        public void StartListening(IPAddress ipAd)
        {
            ipAddress = ipAd;
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 11000);
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(endPoint);
                listener.Listen(100);

                while (true)
                {
                    listenerSignal.Reset();

                    Console.WriteLine("Waiting for connection...");
                    Socket handler = listener.Accept();
                    Console.WriteLine("Listener connected to : " + listener.LocalEndPoint.ToString());

                    HandleConnection(handler);
                    listenerSignal.WaitOne();
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        protected void StartClient(IPAddress ip)
        {
            IPEndPoint endPoint = new IPEndPoint(ip, 11001);

            sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                sendSocket.Connect(endPoint);
                Console.WriteLine("Client connected to: " + sendSocket.LocalEndPoint.ToString());

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

        protected virtual void HandleConnection(object socket)
        {
            listenerSignal.Set();
            try
            {
                Socket handler = (Socket)socket;
                while (true)
                {
                    receiveHandle = new SocketHandle(handler);

                    while (true)
                    {
                        int bytesReceived = receiveHandle.socket.Receive(receiveHandle.buffer);
                        receiveHandle.AppendBytes(bytesReceived);

                        if (receiveHandle.EndOfMessage() == "<EOF>")
                        {
                            break;
                        }
                    }
                    EventHandler<MessageEventArgs> messageReceived = MessageReceived;
                    if (messageReceived != null)
                    {
                        messageReceived.Invoke(null, new MessageEventArgs(receiveHandle.fullBuffer, receiveHandle.endOfMessage));
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        protected void Send(byte[] data)
        {
            byte[] beginning = Encoding.UTF8.GetBytes("<BOF>");
            byte[] ending = Encoding.UTF8.GetBytes("<EOF>");
            byte[] message = new byte[beginning.Length + data.Length + ending.Length];
            Buffer.BlockCopy(beginning, 0, message, 0, beginning.Length);
            Buffer.BlockCopy(data, 0, message, beginning.Length, data.Length);
            Buffer.BlockCopy(ending, 0, message, beginning.Length + data.Length, ending.Length);

            if (IsConnected())
            {
                sendSocket.Send(message);
            }
            else
            {
                //this.sendSocket.Shutdown(SocketShutdown.Both);
                //this.sendSocket.Close();
                //this.StartClient(ipAddress);

                this.Send(data);
                throw new Exception();
            }
        }

        private bool IsConnected()
        {
            return !(sendSocket.Poll(1000, SelectMode.SelectRead) && sendSocket.Available == 0);
        }
    }

    public class SocketHandle
    {
        public Socket socket { get; set; }
        public byte[] buffer { get; set; }
        public byte[] fullBuffer { get; set; }
        public string endOfMessage { get; set; }
        public const int BUFFER_SIZE = 1024;

        public SocketHandle(Socket socket)
        {
            this.socket = socket;
            buffer = new byte[BUFFER_SIZE];
            fullBuffer = new byte[0];
        }

        public void AppendBytes(int bytesReceived)
        {
            byte[] newBuffer = new byte[fullBuffer.Length + bytesReceived];
            Buffer.BlockCopy(fullBuffer, 0, newBuffer, 0, fullBuffer.Length);
            Buffer.BlockCopy(buffer, 0, newBuffer, fullBuffer.Length, bytesReceived);
            fullBuffer = newBuffer;
        }

        public string EndOfMessage()
        {
            if (fullBuffer.Length >= 5)
            {
                byte[] ending = new byte[5];
                Buffer.BlockCopy(fullBuffer, fullBuffer.Length - 5, ending, 0, 5);
                endOfMessage = Encoding.UTF8.GetString(ending);
                return endOfMessage;
            }
            else
            {
                string str = Encoding.UTF8.GetString(fullBuffer);
                throw new Exception("\n\n\n[Buffer destroyed]\n\n\n");
            }
        }

        public void Reset()
        {
            buffer = new byte[BUFFER_SIZE];
            fullBuffer = new byte[0];
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public Message message { get; set; }

        public MessageEventArgs(byte[] msg, string endOfMessage)
        {
            try
            {
                string beginning = Encoding.UTF8.GetString(msg.Take(5).ToArray());


                if (beginning == "<BOF>" && endOfMessage == "<EOF>")
                {
                    byte[] data = new byte[msg.Length - 10];
                    Buffer.BlockCopy(msg, 5, data, 0, msg.Length - 10);
                    string viewMessageTest = Encoding.UTF8.GetString(data);
                    message = BSON.Deserialize<Message>(data);
                    string json = message.ToJson();
                }
                else
                {
                    message = new DestroyedBuffer();
                }
            }
            catch (Exception ex)
            {
                message = new DestroyedBuffer();
                throw ex;
            }
        }
    }
}
