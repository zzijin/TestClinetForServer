using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestClinetForServer.Configuration;
using TestClinetForServer.Network.MessagePack;

namespace TestClinetForServer.Network
{
    class RequestDictionary
    {
        static Dictionary<int, Action<JObject, Action<MsgPack>>> requestDictionary = new Dictionary<int, Action<JObject, Action<MsgPack>>>() {
                { MsgTypeConfiguration.MSG_TYPE_TEST , TestRequest },{MsgTypeConfiguration.MSG_TYPE_HEARTBEAT, HeartbeatBack},
                {MsgTypeConfiguration.MSG_TYPE_CONNECT_FAIL,ConnectFailRequest },
            };

        public static void ProcessRequest(MsgBody msgBody, Action<MsgPack> backAction)
        {
            //if (requestDictionary.ContainsKey(msgBody.MsgType))
            //{
            Console.WriteLine("从服务端接收到数据:" + msgBody.MsgData.ToString());
            requestDictionary[msgBody.MsgType](msgBody.MsgData, backAction);
            //return true;
            //}
            //return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        private static void TestRequest(JObject data, Action<MsgPack> backAction)
        {
            //backAction(RequestBackDictionary.ProcessRequest(MsgConfiguration.MSG_TYPE_TEST, data));
        }

        static void HeartbeatBack(JObject data, Action<MsgPack> backAction)
        {
            backAction(RequestBackDictionary.ProcessRequest(MsgTypeConfiguration.MSG_TYPE_HEARTBEAT,data));
        }

        private static void ConnectFailRequest(JObject data, Action<MsgPack> backAction)
        {
            Console.WriteLine("服务器已满");
        }
    }
}
