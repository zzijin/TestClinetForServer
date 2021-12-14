using Newtonsoft.Json.Linq;
using System;
using TestClinetForServer.Network;
using TestClinetForServer.Configuration;
using System.Threading;
using System.Threading.Tasks;
using TestClinetForServer.Tools;

namespace TestClinetForServer
{
    class Program
    {
        static void Main(string[] args)
        {
            for(int o = 0; o < 3; o++)
            {
                Task.Run(() =>
                {
                    Client _client = new Client();
                    _client.Connect();
                    for (int i = 1; i < 2; i++)
                    {
                        string s = "hi,ni hao,hello";
                        JObject json = new JObject();
                        json.Add("Data", Convert.ToString(s));
                        json.Add("Value", Convert.ToString(i));
                        _client.EnqueueToWaitSendMsgQueue(RequestBackDictionary.ProcessRequest(MsgTypeConfiguration.MSG_TYPE_TEST, json));
                        //Thread.Sleep(5);
                    }
                });
            }
            
            Console.ReadLine();
        }
    }
}
