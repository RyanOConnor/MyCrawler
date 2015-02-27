using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace WebApplication
{
    class Server
    {
        public enum requestType { };
        private TcpListener listener { get; set; }

        public void startServer()
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipLocal = new IPEndPoint(ip, 8888);
            listener = new TcpListener(ipLocal);
            listener.Start();
            waitForClient();
        }

        public void waitForClient()
        {
            object obj = new object();
            listener.BeginAcceptTcpClient(new AsyncCallback(onClientConnect), obj);
        }

        public void onClientConnect(IAsyncResult sync)
        {

            try
            {
                TcpClient clientSocket = default(TcpClient);
                clientSocket = listener.EndAcceptTcpClient(sync);
                HandleRequest clientRequest = new HandleRequest(clientSocket);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            waitForClient();
        }
        
    }

    public class HandleRequest
    {
        public TcpClient client { get; set; }
        public NetworkStream sendStream { get; set; }
        public NetworkStream receiveStream { get; set; }
        
        public HandleRequest(TcpClient client)
        {
            this.client = client;
        }

        public void startClient()
        {
            sendStream = client.GetStream();
            receiveStream = client.GetStream();

            waitForRequest();
        }

        public void waitForRequest()
        {
            byte[] buffer = new byte[client.ReceiveBufferSize];
            receiveStream.BeginRead(buffer, 0, buffer.Length, readCallBack, buffer);
        }

        public void readCallBack(IAsyncResult sync)
        {
            receiveStream = client.GetStream();

            int read = receiveStream.EndRead(sync);
            if(read == 0)
            {
                //stream.Close();
                //client.Close();
                //return;
            }

            byte[] bytes = sync.AsyncState as byte[];
            string data = Encoding.ASCII.GetString(bytes, 0, read);

            string message = handleMessage(data);

            Console.WriteLine(message);

            if (message != "sentToDatabase")
            {
                sendResponse(message);
            }
        }

        public void sendResponse(string message)
        {
            // DO WORK ON DATA HERE, SEND IT BACK TO CLIENT

            byte[] sendBytes = Encoding.ASCII.GetBytes(message);
            sendStream.Write(sendBytes, 0, sendBytes.Length);
            sendStream.Flush();

            waitForRequest();
        }

        public virtual string handleMessage(string message)
        {
            // if first bytes of string == X, then do...
            if(message == "ModifyUser")
            {
                //UserManager.addLinkToUser(message);
            }
            else if(message == "m")
            {

            }

            return "";
        }
    }
}
