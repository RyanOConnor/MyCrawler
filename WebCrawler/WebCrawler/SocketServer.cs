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
        public byte[] buffer { get; set; }
        public const int BUFFER_SIZE = 1024;
        public StringBuilder sb { get; set; }
        public string text { get { return sb.ToString(); } }

        public BufferHandle()
        {
            buffer = new byte[BUFFER_SIZE];
            sb = new StringBuilder();
        }

        public void Reset()
        {
            sb.Clear();
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public MessageEventArgs(string message)
        {
            Message = message;
        }
    }

    public static class SocketServer
    {
        private static Socket Server { get; set; }
        private static ManualResetEvent ConnectDone = new ManualResetEvent(false);
        private static ManualResetEvent SendDone = new ManualResetEvent(false);
        private static ManualResetEvent ReceiveDone = new ManualResetEvent(false);
        public static event EventHandler Connected;
        public static event EventHandler<MessageEventArgs> MessageReceived;
        public static event EventHandler MessageSubmitted;

        public static void StartClient()
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

        public static bool IsConnected()
        {
            return Server.Poll(1000, SelectMode.SelectWrite);
        }

        private static void ConnectCallBack(IAsyncResult result)
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

        public static void Receive()
        {
            try
            {
                BufferHandle bufferHandle = new BufferHandle();
                Server.BeginReceive(bufferHandle.buffer, 0, BufferHandle.BUFFER_SIZE, 0,
                                                    new AsyncCallback(ReceiveCallBack), bufferHandle);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void ReceiveCallBack(IAsyncResult result)
        {
            BufferHandle bufferHandle = (BufferHandle)result.AsyncState;
            int bytesRead = Server.EndReceive(result);
            
            try
            {
                if(bytesRead > 0)
                {
                    bufferHandle.sb.Append(Encoding.ASCII.GetString(bufferHandle.buffer, 0, bytesRead));

                    if(bufferHandle.text.EndsWith("<EOF>"))
                    {
                        string message = bufferHandle.text.Remove(bufferHandle.text.IndexOf("<EOF"));

                        EventHandler<MessageEventArgs> msgReceived = MessageReceived;
                        if(msgReceived != null)
                        {
                            msgReceived(null, new MessageEventArgs(message));
                        }
                        ReceiveDone.Set();
                    }
                    else
                    {
                        Server.BeginReceive(bufferHandle.buffer, 0, BufferHandle.BUFFER_SIZE, 0,
                                                            new AsyncCallback(ReceiveCallBack), bufferHandle);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void Send(string message)
        {
            message += "<EOF>";
            byte[] byteData = Encoding.ASCII.GetBytes(message);

            if(IsConnected())
            {
                Server.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallBack), Server);
            }
            else
            {
                SocketServer.StartClient();
                SocketServer.Send(message);
            }
        }

        public static void SendCallBack(IAsyncResult result)
        {
            try
            {
                Socket server = (Socket)result.AsyncState;
                server.EndSend(result);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            EventHandler msgSubmitted = MessageSubmitted;
            if(msgSubmitted != null)
            {
                msgSubmitted(null, EventArgs.Empty);
            }
            SendDone.Set();
        }
    }
}
