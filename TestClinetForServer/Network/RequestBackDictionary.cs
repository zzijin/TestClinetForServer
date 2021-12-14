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
    class RequestBackDictionary
    {
        static Dictionary<int, Func<JObject, MsgPack>> requestBackDictionary = new Dictionary<int, Func<JObject, MsgPack>>()
        {
            { MsgTypeConfiguration.MSG_TYPE_TEST , TestRequestBack },
            {MsgTypeConfiguration.MSG_TYPE_HEARTBEAT, HeartbeatBack}
        };

        public static MsgPack ProcessRequest(int msgType, JObject data)
        {
            return requestBackDictionary[msgType](data);
        }

        static MsgPack TestRequestBack(JObject data)
        {
            return new MsgPack(new MsgBody(MsgTypeConfiguration.MSG_TYPE_TEST, MsgConfiguration.MSG_FLAG_NO_ENCRYPTION, data));
        }

        private static MsgPack HeartbeatBack(JObject data)
        {
            return new MsgPack(new MsgBody(MsgTypeConfiguration.MSG_TYPE_HEARTBEAT, MsgConfiguration.MSG_FLAG_NO_ENCRYPTION, data));
        }
    }
}
