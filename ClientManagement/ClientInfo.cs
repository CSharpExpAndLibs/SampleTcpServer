using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClientManagement
{
    class ClientInfo
    {
        public int ClientId { get; set; }
        public TcpClient Client { get; set; }
    }
}
