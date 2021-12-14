using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestClinetForServer.Network.MessagePack;

namespace TestClinetForServer.Network
{
    class ConnMsgQueue
    {
        Queue<MsgBody> waitProcessesMsgQueue;
        Queue<MsgPack> waitSendMsgQueue;
        private int waitProcessesMsgQueueMaxNum;
        private int waitSendMsgQueueMaxNum;
        private readonly object waitProcessesMsgQueueLock = new object();
        private readonly object waitSendMsgQueueLock = new object();

        public ConnMsgQueue()
        {
            waitProcessesMsgQueue = new Queue<MsgBody>();
            waitSendMsgQueue = new Queue<MsgPack>();
        }

        public void Clear()
        {
            waitProcessesMsgQueue.Clear();
            waitSendMsgQueue.Clear();
        }

        #region 等待处理消息队列
        /// <summary>
        /// 将任务加入等待处理队列
        /// </summary>
        /// <param name="msgBody"></param>
        public void EnqueueToWaitProcessesMsgQueue(MsgBody msgBody)
        {
            lock (waitProcessesMsgQueueLock)
            {
                waitProcessesMsgQueue.Enqueue(msgBody);
            }
        }

        /// <summary>
        /// 尝试从待处理消息队列中取出任务
        /// </summary>
        /// <param name="msgBody"></param>
        /// <returns>true表示成功取出,false表示取出失败</returns>
        public bool DequeueInWaitProcessesMsgQueue(out MsgBody msgBody)
        {
            lock (waitProcessesMsgQueueLock)
            {
                if (waitProcessesMsgQueue.Count > 0)
                {
                    msgBody = waitProcessesMsgQueue.Dequeue();
                    return true;
                }
                msgBody = null;
                return false;
            }
        }

        public bool GetWaitProcessesMsgQueueState { get => waitProcessesMsgQueue.Count > 0; }

        #endregion

        #region 等待发送消息队列方法
        /// <summary>
        /// 将消息加入等待发送队列
        /// </summary>
        /// <param name="msgPack"></param>
        public void EnqueueToWaitSendMsgQueue(MsgPack msgPack)
        {
            lock (waitSendMsgQueueLock)
            {
                waitSendMsgQueue.Enqueue(msgPack);
            }
        }

        /// <summary>
        /// 尝试从待发送消息队列中取出消息
        /// </summary>
        /// <param name="msgPack"></param>
        /// <returns></returns>
        public bool DequeueInWaitSendMsgQueue(out MsgPack msgPack)
        {
            lock (waitSendMsgQueueLock)
            {
                if (waitProcessesMsgQueue.Count > 0)
                {
                    msgPack = waitSendMsgQueue.Dequeue();
                    return true;
                }
                msgPack = null;
                return false;
            }
        }

        /// <summary>
        /// 从待发送消息队列中移除首消息
        /// </summary>
        /// <param name="msgPack"></param>
        /// <returns></returns>
        public void DequeueInWaitSendMsgQueue()
        {
            waitSendMsgQueue.Dequeue();
        }

        /// <summary>
        /// 尝试获取待发送消息队列首消息包
        /// </summary>
        /// <param name="msgPack"></param>
        /// <returns></returns>
        public bool PeekInWaitSendMsgQueue(out MsgPack msgPack)
        {
            if (waitSendMsgQueue.Count > 0)
            {
                msgPack = waitSendMsgQueue.Peek();
                return true;
            }
            msgPack = null;
            return false;
        }

        public bool GetWaitSendMsgQueueState { get => waitSendMsgQueue.Count > 0; }
        #endregion
    }
}
