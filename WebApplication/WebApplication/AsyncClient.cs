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
    class AsyncClient
    {
        public SocketHandle sendSocket { get; private set; }
        public IPAddress ipAddress { get; set; }
        private ManualResetEvent ConnectDone = new ManualResetEvent(false);
        public event EventHandler ClientConnected;
        public event EventHandler MessageSubmitted;

        public void StartClient(IPAddress ipAdd)
        {
            ipAddress = ipAdd;
            IPEndPoint endPoint = new IPEndPoint(ipAdd, 11001);

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
            return !(sendSocket.socket.Poll(1000, SelectMode.SelectRead) && sendSocket.socket.Available == 0);
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
                sendSocket.socket.BeginSend(message, 0, message.Length, 0, new AsyncCallback(SendCallBack), sendSocket.socket);
            }
            else
            {
                //this.sendSocket.socket.Shutdown(SocketShutdown.Both);
                //this.sendSocket.socket.Close();

                //this.StartClient(ipAddress);

                while (!IsConnected()) ;

                this.Send(data);
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
}
