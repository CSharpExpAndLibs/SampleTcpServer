using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace EventLoopControllerModel
{
    public class MessageQueue
    {
        Queue<Message> queue = new Queue<Message>();
        ManualResetEvent isQueued = new ManualResetEvent(false);

        public Message Dequeue()
        {
            isQueued.WaitOne();
            lock (queue)
            {
                var ret = queue.Dequeue();
                if (queue.Count == 0)
                    isQueued.Reset();
                return ret;
            }
        }

        public void Enqueue(Message message)
        {
            lock (queue)
            {
                queue.Enqueue(message);
                isQueued.Set();
            }
        }
    }
}
