using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClientManagement
{
    public class ClientManager
    {
        /// <summary>
        /// ClientInfo.clientのハッシュをキーにしてClientInfoを
        /// 格納する。
        /// </summary>
        Dictionary<int, ClientInfo> clientList = new Dictionary<int, ClientInfo>();

        /// <summary>
        /// 登録されたクライアントに付与する一意のID
        /// </summary>
        int nextClientId = 1;

        /// <summary>
        /// 指定されたclientをリストに加える
        /// </summary>
        /// <param name="client">追加するclient</param>
        public void Add(TcpClient client)
        {
            ClientInfo inf;
            int k = client.GetHashCode();
            if (!clientList.TryGetValue(k, out inf))
            {
                clientList.Add(
                    k,
                    new ClientInfo() { Client = client, ClientId = nextClientId++, });
            }
        }

        /// <summary>
        /// 指定されたclientをリストから削除する
        /// </summary>
        /// <param name="client">削除するclient</param>
        /// <param name="isDispose">clientを破棄するかどうか</param>
        /// <returns>成功:true</returns>
        public (bool, string) Remove(TcpClient client, bool isDispose = true)
        {
            ClientInfo inf;
            int k = client.GetHashCode();
            if (!clientList.TryGetValue(k, out inf))
            {
                return (false, "指定されたクライアントは存在しません");
            }
            clientList.Remove(k);

            if (isDispose)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"クライアントの破棄に失敗しましたが処理は継続します:" +
                        $"発生した例外:{e.Message}");
                }
            }

            return (true, null);
        }


        /// <summary>
        /// 全てのクライアントをリストから削除する
        /// </summary>
        /// <param name="isDispose">クライアントを破棄するかどうか</param>
        /// <returns></returns>
        public void RemoveAll(bool isDispose = true)
        {
            foreach (var k in clientList.Keys.ToArray())
            {
                var inf = clientList[k];
                clientList.Remove(k);
                if (isDispose)
                {
                    try
                    {
                        inf.Client.Dispose();
                    }
                    catch
                    {

                    }
                }
            }
        }

        /// <summary>
        /// 指定されたクライアントのIDを返す
        /// </summary>
        /// <param name="client">対象のクライアント</param>
        /// <returns>成功:ClientID、失敗:-1</returns>
        public int GetClientId(TcpClient client)
        {
            ClientInfo inf;
            if (!clientList.TryGetValue(client.GetHashCode(), out inf))
            {
                return -1;
            }
            return inf.ClientId;
        }
    }
}
