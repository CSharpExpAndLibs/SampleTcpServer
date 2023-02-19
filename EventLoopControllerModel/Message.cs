using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace EventLoopControllerModel
{
    public class Message
    {
        public string RecieveMsg { get; set; } = string.Empty;
        public Reply Reply { get; set; } = new Reply();
        public AutoResetEvent NotifyEnd = new AutoResetEvent(false);
    }
}
