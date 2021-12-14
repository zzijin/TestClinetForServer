using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClinetForServer.Network
{
    class ClinetInfo
    {
        #region 链接的固有信息(该链接初始化时即固定，直到到程序结束都不会发送改变)
        /// <summary>
        /// 该链接在链接池中的地址
        /// </summary>
        //private int connIndex;
        #endregion

        #region 本次连接的信息
        /// <summary>
        /// 当前连接在已使用链接链表中的节点信息
        /// </summary>
        //private LinkedListNode<int> connNode;
        /// <summary>
        /// 当前链接使用起始时间
        /// </summary>
        private DateTime connStartUseTime;
        #endregion

        #region 链接的统计信息
        /// <summary>
        /// 链接使用次数
        /// </summary>
        private long connTotalUsedCount;
        /// <summary>
        /// 链接总接收字节数
        /// </summary>
        private long connTotalReceiveBytes;
        /// <summary>
        /// 链接总发送字节数
        /// </summary>
        private long connTotalSendBytes;
        /// <summary>
        /// 链接总使用时间
        /// </summary>
        private TimeSpan connTotalUseTime;
        /// <summary>
        /// 链接总解析消息数量
        /// </summary>
        private long connTotalParseMsg;
        /// <summary>
        /// 链接总发送消息数量
        /// </summary>
        private long connTotalSendMsg;
        /// <summary>
        /// 处理信息个数
        /// </summary>
        private long connTotalProcessMsg;
        #endregion

        public long ConnTotalUseCount { get => connTotalUsedCount; }
        public long ConnTotalReceiveBytes { get => connTotalReceiveBytes; }
        public long ConnTotalSendBytes { get => connTotalSendBytes; }
        public TimeSpan ConnTotalUseTime { get => connTotalUseTime; }
        public long ConnTotalParseMsg { get => connTotalParseMsg; }
        public long ConnTotalSendMsg { get => connTotalSendMsg; }
        public long ConnTotalProcessMsg { get => connTotalProcessMsg; }

        public ClinetInfo()
        {
            connTotalUsedCount = 0;
            connTotalReceiveBytes = 0;
            connTotalSendBytes = 0;
            connTotalUseTime = new TimeSpan(0);
            connTotalParseMsg = 0;
            connTotalSendMsg = 0;
            connTotalProcessMsg = 0;
        }

        public void Connect()
        {
            connStartUseTime = DateTime.Now;
            connTotalUsedCount++;
        }

        public void DisConnect(string msg)
        {
            //this.connNode = null;
            connTotalUseTime += (DateTime.Now - connStartUseTime);
            Console.WriteLine("链接断开,使用时长:" + (long)(DateTime.Now - connStartUseTime).TotalMilliseconds
                + "毫秒;该链接总计接收:" + connTotalReceiveBytes + ";该链接总计发送:" + connTotalSendBytes
                + ";解析消息个数:" + connTotalParseMsg + ";处理消息个数:" + connTotalProcessMsg + ";断开原因:{" + msg+"}");
        }

        #region 添加统计消息
        public void AddConnTotalReceiveBytes(int addSize)
        {
            connTotalReceiveBytes += addSize;
            Console.WriteLine("接收消息,大小:" + addSize + ";该链接总计接收:" + connTotalReceiveBytes);
        }

        public void AddConnTotalSendBytes(int addSize)
        {
            connTotalSendBytes += addSize;
            Console.WriteLine("发送消息,大小:" + addSize + ";该链接总计发送:" + connTotalSendBytes);
        }

        public void AddConnTotalParseMsg()
        {
            connTotalParseMsg ++;
            Console.WriteLine("处理消息个数:" + connTotalParseMsg);
        }

        public void AddConnTotalProcessMsg()
        {
            connTotalProcessMsg++;
            Console.WriteLine("处理消息个数:" + connTotalProcessMsg);
        }

        public void AddConnTotalSendMsg()
        {
            connTotalSendMsg ++;
            Console.WriteLine("发送消息总数:" + connTotalSendMsg);
        }
        #endregion
    }
}
