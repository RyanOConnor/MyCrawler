using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace WebCrawler
{
    public class SocketServer
    {
        private IPAddress ipAddress { get; set; }
        public Socket sendSocket { get; set; }
        public event EventHandler ServerConnected;
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<ClientConnectedArgs> ClientConnected;
        protected ManualResetEvent listenerSignal = new ManualResetEvent(false);

        public void StartClient(string ip)
        {
            ipAddress = IPAddress.Parse(ip);
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 11000);

            sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                sendSocket.Connect(endPoint);
                Console.WriteLine("Client connected to: " + sendSocket.LocalEndPoint.ToString());

                EventHandler<ClientConnectedArgs> connectedHandler = ClientConnected;
                if (connectedHandler != null)
                {
                    connectedHandler(null, new ClientConnectedArgs(ipAddress));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public void StartListening()
        {
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 11001);

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(endPoint);
                listener.Listen(100);

                while(true)
                {
                    listenerSignal.Reset();

                    Console.WriteLine("Waiting for connection...");
                    Socket handler = listener.Accept();
                    Console.WriteLine("Listener onnected to : " + listener.LocalEndPoint.ToString());

                    EventHandler connectedHandler = ServerConnected;
                    if (connectedHandler != null)
                    {
                        connectedHandler(null, EventArgs.Empty);
                    }

                    HandleConnection(handler);
                    listenerSignal.WaitOne();
                }
            }
            catch(SocketException ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public void HandleConnection(Socket socket)
        {
            try
            {
                Socket handler = socket;
                while(true)
                {
                    SocketHandle handle = new SocketHandle(handler);

                    while (true)
                    {
                        int bytesReceived = handle.socket.Receive(handle.buffer);
                        handle.AppendBytes(bytesReceived);

                        if (handle.EndOfMessage() == "<EOF>")
                        {
                            break;
                        }
                    }

                    EventHandler<MessageEventArgs> messageReceived = MessageReceived;
                    if (messageReceived != null)
                    {
                        messageReceived.Invoke(null, new MessageEventArgs(handle.fullBuffer, handle.endOfMessage));
                    }
                }
            }
            catch(SocketException ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;

                //socket.Close();
                //socket.Shutdown(SocketShutdown.Both);
                //listenerSignal.Set();
            }
        }

        public void Send(byte[] data)
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
                //SocketClient.Instance.sendSocket.Shutdown(SocketShutdown.Both);
                //SocketClient.Instance.sendSocket.Close();
                //SocketClient.Instance.StartClient(ipAddress);

                Send(message);
                throw new Exception();
            }
        }

        public bool IsConnected()
        {
            return !(sendSocket.Poll(1000, SelectMode.SelectRead) && sendSocket.Available == 0);
        }
    }

    public class SocketHandle
    {
        public Socket socket { get; set; }
        public byte[] buffer { get; set; }
        public const int BUFFER_SIZE = 1024;
        public MemoryStream byteStream { get; set; }
        public byte[] fullBuffer { get; set; }
        public string endOfMessage { get; set; }

        public SocketHandle(Socket socket)
        {
            this.socket = socket;
            buffer = new byte[BUFFER_SIZE];
            byteStream = new MemoryStream();
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
                throw new Exception("\n\n\n[Buffer destroyed]\n\n\n");
            }
        }

        public void Reset()
        {
            byteStream = new MemoryStream();
        }
    }

    public class ClientConnectedArgs : EventArgs
    {
        public IPAddress ipAddress { get; set; }
        public ClientConnectedArgs(IPAddress ip)
        {
            ipAddress = ip;
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
                    message = BSON.Deserialize<Message>(data);
                    
                    if(message is RecordMessage)
                    {
                        RecordMessage rc = message as RecordMessage;
                        //Console.WriteLine("Received: " + rc.htmlRecord.Id);
                        //Thread.Sleep(1000);
                    }
                }
                else
                {
                    message = new DestroyedBuffer();
                }
            }
            catch(Exception ex)
            {
                message = new DestroyedBuffer();
                throw ex;
            }
        }
    }
}
