using System;
using System.Net.Sockets;

namespace Chat
{
    class tcpEventArgs:EventArgs
    {
        private TcpClient sock;
        public TcpClient clientSock
        {
            get { return sock; }
            set { sock = value; }
        }

        public tcpEventArgs(TcpClient tcpClient)
        {
            sock = tcpClient;
        }

    }
}
