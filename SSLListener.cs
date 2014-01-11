using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;

namespace AsyncSSLServer
{
    class SSLListener
    {
        private ManualResetEvent mre = new ManualResetEvent(false);

        public SSLListener(int port)
        {
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            listenSocket.Bind(localEndPoint);
            listenSocket.Listen(500);

            while (true)
            {
                mre.Reset();
                listenSocket.BeginAccept(AcceptCallback, listenSocket);
                mre.WaitOne();
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            mre.Set();
            Console.WriteLine("Accepting callback");
            Socket listener = (Socket)ar.AsyncState;
            Console.WriteLine("Got Listener");
            Socket handler = listener.EndAccept(ar);

            Console.WriteLine("Accepting connection from " + handler.RemoteEndPoint.AddressFamily.ToString());

            ConnectionManager cm = new ConnectionManager(handler);

            handler.BeginReceive(cm.Buffer, 0, cm.Buffer.Length, 0, new AsyncCallback(ReadCallBack), cm);
        }

        private void ReadCallBack(IAsyncResult ar)
        {
            ConnectionManager cm = (ConnectionManager)ar.AsyncState;
            int numRead = 0;
            try
            {
                numRead = cm.Handler.EndReceive(ar);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Client has closed");
            }

            if (numRead > 0)
            {
                cm.UpdateMessage(numRead);
                if (cm.FinishedReceivingMessage())
                {
                    if (!cm.Validate())
                        return; // invalid message - do something neat here
                    else
                    {
                        //send back the appropriate message
                        Console.WriteLine("Received: " + Encoding.ASCII.GetString(cm.Message));
                        byte[] responsePacket = cm.GetResponsePacket();
                        cm.ClearBuffer();
                        Console.WriteLine("Sending: " + Encoding.ASCII.GetString(responsePacket));
                        cm.Handler.BeginSend(responsePacket, 0, responsePacket.Length, 0, new AsyncCallback(SendCallback), cm);
                    }
                }
                else
                {
                    cm.Handler.BeginReceive(cm.Buffer, 0, cm.Buffer.Length, 0, new AsyncCallback(ReadCallBack), cm);
                }
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                ConnectionManager cm = (ConnectionManager)ar.AsyncState;
                int bytesSent = cm.Handler.EndSend(ar);
                cm.Handler.BeginReceive(cm.Buffer, 0, cm.Buffer.Length, 0, new AsyncCallback(ReadCallBack), cm);

            }
            catch (Exception e)
            {
                Console.WriteLine("Connection was closed");
            }
        }
    }

    public class ConnectionManager
    {
        private byte[] buffer;
        private Socket handler;
        private byte[] message;
        private int messageType;

        public byte[] Buffer
        {
            get { return buffer; }
        }

        public Socket Handler
        {
            get { return handler; }
        }

        public byte[] Message
        {
            get { return message; }
        }

        public ConnectionManager(Socket handler)
        {
            buffer = new byte[1024];
            message = new byte[0];
            this.handler = handler;
        }

        public void ClearBuffer()
        {
            buffer = new byte[1024];
            message = new byte[0];
            messageType = -1;
        }


        public bool Validate()
        {
            messageType = NetworkingMethods.ValidateMessageFormat(message);
            return messageType != -1;
        }

        public void AddBytes(byte[] msg)
        {
            NetworkingMethods.SpliceByteArray(message, msg);
        }

        public void AddBytes(byte[] msg, int numRead)
        {
            message = NetworkingMethods.SpliceByteArray(message, msg, numRead);
        }

        public bool FinishedReceivingMessage()
        {
            if (message == null)
                return false;
            string content = Encoding.ASCII.GetString(NetworkingMethods.GetPartOfArray(message, message.Length - NetworkingMethods.EndOfMessage.Length, NetworkingMethods.EndOfMessage.Length));
            string eom = Encoding.ASCII.GetString(NetworkingMethods.EndOfMessage);
            return NetworkingMethods.GetPartOfArray(message, message.Length - NetworkingMethods.EndOfMessage.Length, NetworkingMethods.EndOfMessage.Length).SequenceEqual(NetworkingMethods.EndOfMessage);
        }

        public byte[] GetResponsePacket()
        {
            byte[] content = NetworkingMethods.GetContentFromMessage(message);
            string val = Encoding.ASCII.GetString(content);
            if (val == "hello")
            {
                return Encoding.ASCII.GetBytes("response");
            }
            else if (val == "again")
            {
                return Encoding.ASCII.GetBytes("yup");
            }
            return new byte[] { (byte)'T' };
        }

        public void UpdateMessage(int numRead)
        {
            string content = Encoding.ASCII.GetString(this.buffer);
            AddBytes(this.buffer, numRead);
        }
    }
}
