using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClinetForServer.Configuration
{
    class ConnConfiguration
    {
        /// <summary>
        /// 单链接接收缓冲区大小，单位byte
        /// </summary>
        public static int RECEIVE_BUFFER_SIZE = 1024*10;
        /// <summary>
        /// 单链接发送缓冲区大小，单位byte
        /// </summary>
        public static int SEND_BUFFER_SIZE = 1000;
        /// <summary>
        ///  链接发送时间间隔，单位ms
        /// </summary>
        public static int SEND_MAX_INTERVAL = 500;
        /// <summary>
        ///  链接无消息休眠时间
        /// </summary>
        public static int NO_MSG_THREAD_SLEEP_MIN_TIME = 500;
    }
}
