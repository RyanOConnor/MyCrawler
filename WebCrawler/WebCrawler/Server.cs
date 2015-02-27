using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Threading;

namespace WebCrawler
{
    static class Server2
    {
        private static TcpClient client { get; set; }
        private static NetworkStream sendStream { get; set; }
        private static NetworkStream receiveStream { get; set; }

        public static void startClient()
        {
            client = new TcpClient();
            waitForServer();
        }

        public static void waitForServer()
        {
            IPAddress ipAd = IPAddress.Parse("127.0.0.1");

            object obj = new object();
            client.BeginConnect(ipAd, 8888, new AsyncCallback(onServerConnect), obj);
        }

        public static void onServerConnect(IAsyncResult sync)
        {
            Console.WriteLine("Server connected...");

            try
            {
                sendStream = client.GetStream();
                receiveStream = client.GetStream();
                byte[] buffer = new byte[client.ReceiveBufferSize];
                receiveStream.BeginRead(buffer, 0, buffer.Length, readCallBack, buffer);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void waitForResponse()
        {
            receiveStream = client.GetStream();
            byte[] buffer = new byte[client.ReceiveBufferSize];
            receiveStream.BeginRead(buffer, 0, buffer.Length, readCallBack, buffer);
        }

        public static void readCallBack(IAsyncResult sync)
        {
            receiveStream = client.GetStream();
            try
            {
                int read = receiveStream.EndRead(sync);
                if (read == 0)
                {
                    //stream.Close();
                    //client.Close();
                    //return;
                }
                byte[] buffer = sync.AsyncState as byte[];
                string data = Encoding.Default.GetString(buffer, 0, read);
                handleMessage(data);
            }
            catch(Exception ex)
            {
                throw ex;
            }

            waitForResponse();
        }

        public static void handleMessage(string message)
        {
            if (message == "status")
                sendCrawlerStatus();
            else
                assembleIncomingObject(message);
        }

        public static void sendMessage(string message)
        {
            sendStream = client.GetStream();
            byte[] outStream = Encoding.ASCII.GetBytes(message);
            sendStream.Write(outStream, 0, outStream.Length);
            sendStream.Flush();
        }

        public static void assembleIncomingObject(string data)
        {
            // Create InitialPage object from listener
            // deserialize JSON string into HTMLPage components

            // Call allowNextTransmission() once finished

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HTMLPage));
            MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
            HTMLPage htmlPage = ser.ReadObject(stream) as HTMLPage;

            Console.WriteLine("assembled {0}", htmlPage.domainURL.AbsoluteUri);

            WebCrawler.enqueue(htmlPage);
            allowNextTransmission();
        }

        public static void disassembleOutgoingObject(HTMLPage page)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HTMLPage));
            MemoryStream stream = new MemoryStream();
            ser.WriteObject(stream, page);
            string JSON = Encoding.ASCII.GetString(stream.ToArray());
            sendMessage(JSON);
        }

        public static void allowNextTransmission()
        {
            sendMessage("next");
        }

        public static void sendCrawlerStatus()
        {
            
        }

        /*public static void start()
        {
            client = new TcpClient();
        }

        public static void connect()
        {
            client.Connect("127.0.0.1", 8888);
        }

        public static void sendMessage(string message)
        {
            if(string.IsNullOrEmpty(message))
            {
                return;
            }
            else
            {
                NetworkStream netStream = client.GetStream();
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                netStream.Write(buffer, 0, buffer.Length);
                netStream.Flush();
            }
        }

        public static void closeConnection()
        {
            client.Close();
        }

        public static string recieveMessage()
        {
            StringBuilder message = new StringBuilder();
            NetworkStream serverStream = client.GetStream();
            serverStream.ReadTimeout = 100;
            while(true)
            {
                if (serverStream.DataAvailable)
                {
                    int read = serverStream.ReadByte();
                    if (read > 0)
                        message.Append((char)read);
                    else
                        break;
                }
                else if (message.ToString().Length > 0)
                    break;
            }
            return message.ToString();
        }*/
    }

    static class Server
    {
        static TcpClient client;
        static TcpListener listener;

        public static void start()
        {
            Thread thread = new Thread(new ThreadStart(tcpListener));
            thread.Start();
        }

        public static void tcpListener()
        {
            // TcpListener listening on specific port for specific local IP address.
            //      Maybe it's own thread to always be accepting data?
            //      Maybe set as a background process thread?
            // Send data to assembleIncomingPage
            // If StatusCheck then send it crawlerStatus

            IPAddress ip = IPAddress.Parse("127.0.0.1");
            listener = new TcpListener(ip, 8888);
            listener.Start();
            while (true)
            {
                Thread t = new Thread(handleIncomingMessage);
                Socket s = listener.AcceptSocket();
                t.Start(s);
            }
        }

        public static void tcpClient(string message)
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            client = new TcpClient();
            client.Connect(ip, 8888);
            NetworkStream stream = client.GetStream();
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }

        public static void handleIncomingMessage(object data)
        {
            /*NetworkStream stream = client.GetStream();
            byte[] bytes = new byte[client.ReceiveBufferSize];
            stream.Read(bytes, 0, (int)client.ReceiveBufferSize);
            string messagee = Encoding.ASCII.GetString(bytes);*/

            Socket s = (Socket)data;
            Console.WriteLine("Connection accepted from " + s.RemoteEndPoint);

            byte[] bytes = new byte[1000];
            int count = s.Receive(bytes);
            ASCIIEncoding encoding = new ASCIIEncoding();
            string message = encoding.GetString(bytes, 0, count);

            if (message == "status")
                sendStatus();
            else
                assembleIncomingObject(message);
        }

        public static void assembleIncomingObject(string data)
        {
            // Create InitialPage object from listener
            // deserialize JSON string into HTMLPage components

            // Call allowNextTransmission() once finished

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HTMLPage));
            MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
            HTMLPage htmlPage = ser.ReadObject(stream) as HTMLPage;

            Console.WriteLine("assembled {0}", htmlPage.domainURL.AbsoluteUri);

            WebCrawler.enqueue(htmlPage);
            allowNextTransmission();
        }

        public static void disassembleOutgoingObject(HTMLPage page)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HTMLPage));
            MemoryStream stream = new MemoryStream();
            ser.WriteObject(stream, page);
            string JSON = Encoding.ASCII.GetString(stream.ToArray());
            tcpClient(JSON);
        }

        public static void allowNextTransmission()
        {
            // Send application response so it can stop busy waiting and send next set of crawl information
            Console.WriteLine("\nFetching next transmission...");
            tcpClient("next");
        }

        public static void sendStatus()
        {
            // send crawlerStatus to web application
            tcpClient(WebCrawler.status.ToString());
        }
    }
}
