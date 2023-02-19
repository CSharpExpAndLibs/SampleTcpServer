using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventLoopControllerModel
{
    enum InternalState
    {
        State1,
        State2,
        State3,
        State4,
        InTransToState1,
        InTransToState2,
        InTransToState3,
        InTransToState4,
    }

    class Program
    {
        static Server server = null;
        static MessageQueue messageQueue = null;
        static InternalState state = InternalState.State1;

        static void Usage()
        {
            Console.WriteLine("Usage:EventLoopClientModel [port]");
            Console.WriteLine("  port: サーバーのリッスンポート(Default=12345)");
        }

        /// <summary>
        /// ServerのClientThreadから呼ばれるcallback
        /// </summary>
        /// <param name="clientMessage">Clientが送信したメッセージ</param>
        /// <returns>処理結果</returns>
        static Reply SendMessage(string clientMessage)
        {
            var recieve = new Message() { RecieveMsg = clientMessage, };
            messageQueue.Enqueue(recieve);
            recieve.NotifyEnd.WaitOne();

            return recieve.Reply;
        }

        static string GetState()
        {
            return state.ToString();
        }

        static string[] ParseMessage(string msg)
        {
            return msg.Split(new char[] { ':' });
        }

        /// <summary>
        /// state "dst"へ遷移する処理をモデル化した。
        /// </summary>
        /// <param name="dst">行先の状態</param>
        /// <param name="intermidiate">中間状態</param>
        /// <param name="possible">行先への遷移が許可されている状態</param>
        /// <param name="msg">クライアントからのメッセージ</param>
        /// <returns>成否</returns>
        static bool TransState(InternalState dst,
            InternalState intermidiate, InternalState possible, Message msg)
        {
            lock (messageQueue)
            {
                if (state == intermidiate)
                {
                    msg.Reply.ReplyMessage = "Busy";
                    msg.NotifyEnd.Set();
                    return false;
                }
                if (state != possible)
                {
                    msg.Reply.ReplyMessage = "InvalidState";
                    msg.NotifyEnd.Set();
                    return false;
                }
                state = intermidiate;
            }

            // 別スレッドで5秒かけて遷移するモデル
            Task.Run(() =>
            {
                Thread.Sleep(5000);
                lock (messageQueue)
                {
                    state = dst;
                }
                Console.WriteLine($"{dst}への遷移が完了しました。");
            });

            msg.Reply.ReplyMessage = "Success";
            msg.NotifyEnd.Set();

            return true;
        }

        static void Main(string[] args)
        {
            if (args.Length > 1) 
            {
                Usage();
                return;
            }

            int port = 12345;
            if (args.Length == 1)
            {
                if (!int.TryParse(args[0],out port))
                {
                    Usage();
                    return;
                }
            }

            // Server開始
            server = new Server(SendMessage, GetState);
            if (!server.Start())
            {
                Console.WriteLine("サーバー開始がエラーになりました。");
                return;
            }

            // Event待ちLoopに入る。
            // Loopの処理仕様は下記の通り。
            // 1. 遷移規則はstate1->state2->state3->state1->...である。規則外の
            // 遷移コマンドに対しては、"InvalidState"を返す。
            // 2. In transition状態は遷移先の状態を用いて"InTransToState2"のように表す。
            // StateXへの遷移処理が非同期に任されて状態がInTransitionStateXである時、
            // Loopに再度StateXへの遷移命令が来た時には"Busy"を返す。
            // 3. FinishProcessコマンドを受け取った時はReply.IsTermServerをtrueに設定
            // して返し、Loopを抜ける。
            // 
            // 
            messageQueue = new MessageQueue();
            while (true)
            {
                var msg = messageQueue.Dequeue();
                var commands = ParseMessage(msg.RecieveMsg);

                Console.Write($"EventLoop:cmd={commands[0]}");
                for (int i = 1; i < commands.Length; i++)
                {
                    Console.Write($",arg[{i - 1}]={commands[i]}");
                }
                Console.WriteLine();

                if (commands[0] == "GotoState1")
                {
                    if (!TransState(InternalState.State1, InternalState.InTransToState1,
                        InternalState.State4, msg))
                    {
                        continue;
                    }
                }
                else if (commands[0] == "GotoState2")
                {
                    if (!TransState(InternalState.State2, InternalState.InTransToState2,
                        InternalState.State1, msg))
                    {
                        continue;
                    }
                }
                else if (commands[0] == "GotoState3")
                {
                    if (!TransState(InternalState.State3, InternalState.InTransToState3,
                        InternalState.State2, msg))
                    {
                        continue;
                    }
                }
                else if (commands[0] == "GotoState4")
                {
                    if (!TransState(InternalState.State4, InternalState.InTransToState4,
                        InternalState.State3, msg))
                    {
                        continue;
                    }
                }
                else if(commands[0] == "FinishProcess")
                {
                    msg.Reply.ReplyMessage = "Success";
                    msg.Reply.IsTermServer = true;
                    msg.NotifyEnd.Set();
                    break;
                }
                else
                {
                    Console.WriteLine($"コマンド'{commands[0]}'には対応してません。");
                }
            }
        }
    }
}
