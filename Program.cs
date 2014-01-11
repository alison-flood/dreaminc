using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace AsyncSSLServer
{
    class Program
    {
        static void Main(string[] args)
        {
            new SSLListener(13000);
        }
    }
}
