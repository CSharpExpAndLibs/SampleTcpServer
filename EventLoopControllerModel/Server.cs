using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ClientManagement;

namespace EventLoopControllerModel
{
    /// <summary>
    /// このサーバークラスの仕様は下記の通り。<br/>
    /// 1. 指定されたポートをリッスンし、接続したクライアント毎に個別のタスクで応答する。<br/>
    /// 2. BeginAcceptTcpClient()に渡すCallbackであるCommunicateWithClient()では、クライアントから
    /// 受信したデータを文字列に変換し、その文字列を引数にしてホスト（Serverを作成した人）から設定された
    /// コールバックであるControllerCallback()を呼ぶ。<br/>
    /// 3. CommunicateWithClient()はControllerCallback()の戻り値を整形してクライアントに返信する。<br/>
    /// 4. CommunicateWithClient()はControllerCallback()の戻り値によりTerminationを指示された時は
    /// Stop()メソッドを呼び、接続中のクライアントを全て切断してからリスナを閉じて終了する。<br/>
    /// </summary>
    class Server
    {
        public Func<string, Reply> ControllerCallback { get; set; } = null;
        public Func<string> GetState { get; set; } = null;
        public int Port { get; set; } = 12345;
        public string Ip { get; set; } = "127.0.0.1";

        private TcpListener server = null;
        private Task serverTask = null;
        private AutoResetEvent termServerEvent = new AutoResetEvent(false);
        private ClientManager clientManager = new ClientManager();

        public Server(Func<string, Reply> callback, Func<string> getstate, int port = 12345, string ip = "127.0.0.1")
        {
            ControllerCallback = callback;
            GetState = getstate;
            Port = port;
            Ip = ip;
        }

        public bool Start()
        {
            if (ControllerCallback == null)
            {
                Console.WriteLine("ControllerCallbackが設定されていません。");
                return false;
            }

            try
            {
                server = new TcpListener(IPAddress.Parse(Ip), Port);
                server.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine($"サーバーの起動に失敗しました:例外発生:{e.Message}");
                return false;
            }

            // Server Task起動
            serverTask = Task.Run(() =>
            {
                Console.WriteLine($"サーバータスクが開始されました。Port={Port}");
                while (true)
                {
                    var r = server.BeginAcceptTcpClient(new AsyncCallback(CommunicateWithClient), server);
                    var evtIdx = WaitHandle.WaitAny(new WaitHandle[] { r.AsyncWaitHandle, termServerEvent });
                    if (evtIdx == 1)
                        break;
                }
            });

            // serverTaskへのタスクスイッチ
            Thread.Sleep(0);

            return true;
        }

        void CommunicateWithClient(IAsyncResult result)
        {
            TcpClient client = null;
            try
            {
                client = ((TcpListener)result.AsyncState).EndAcceptTcpClient(result);
            }
            catch
            {
                // BeginAcceptの親クラスであるListenerが閉じられると
                // このCallbackが呼ばれる。この条件でEndAcceptを呼ぶと
                // 例外が起きるので、この例のようにcatchすること。
                // これを知らないと間違いなく嵌ります。
                return;
            }

            // clientManagerへ登録
            lock (clientManager)
                clientManager.Add(client);

            int clientId = clientManager.GetClientId(client);
            Console.WriteLine($"新しいクライアントを開始しました:ClientId={clientId}");

            var stream = client.GetStream();
            var readData = new byte[256];

            // 相手が接続を切るか終了命令を受け取るまで処理を続ける
            while (true)
            {
                try
                {
                    var len = stream.Read(readData, 0, readData.Length);
                    if (len == 0)
                    {
                        Console.WriteLine($"クライアント[{clientId}]は接続を切断されました");
                        break;
                    }
                    var str = Encoding.UTF8.GetString(readData, 0, len);
                    Console.WriteLine($"クライアント[{clientId}]は'{str}'を受け取りました");

                    var r = ControllerCallback(str);
                    var writeData = Encoding.UTF8.GetBytes(r.ReplyMessage);
                    stream.Write(writeData, 0, writeData.Length);

                    if (r.IsTermServer)
                    {
                        // Serverを終了して全てのリソースを破棄する。
                        // ここでbreakすると、この'client'自身は既にClientManager
                        // から削除された状態でRemove(client)が実行される。しかしRemove()は
                        // 例外をTrapするので問題ない。
                        Console.WriteLine($"クライアント[{clientId}]は'TermServer'を受け取りました");
                        Stop();
                        break;
                    }
                }
                catch
                {
                    Console.WriteLine($"クライアント[{clientId}]は接続を切断されました");
                    break;
                }
            }

            lock (clientManager)
                clientManager.Remove(client);
        }

        void Stop()
        {
            lock (clientManager)
            {
                clientManager.RemoveAll();
            }
            termServerEvent.Set();
            serverTask.Wait();
            server.Stop();
            Console.WriteLine("TermServer:Serverを終了しました。");
        }
    }
}
