using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLoopControllerModel
{
    public class Reply
    {
        public bool IsTermServer { get; set; } = false;
        public string ReplyMessage { get; set; }
    }
}
