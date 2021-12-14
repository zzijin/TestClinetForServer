using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestClinetForServer.Configuration;
using TestClinetForServer.Network.MessagePack;

namespace TestClinetForServer.Network
{
    class Client
    {
        Socket _socket;
        ConnBufferManager connBufferManager;
        ConnMsgQueue connMsgQueue;
        MsgPack msgPack;
        SocketAsyncEventArgs sendEventArgs;
        SocketAsyncEventArgs receiveEventArgs;
        private Action<MsgPack> EnqueueToThisConnWaitSendMsgQueue;
        ClinetInfo connInfo;
        ClientToken connToken;

        public Client()
        {
            connBufferManager = new ConnBufferManager();
            connMsgQueue = new ConnMsgQueue();
            msgPack = new MsgPack();
            connInfo = new ClinetInfo();
            EnqueueToThisConnWaitSendMsgQueue = EnqueueToWaitSendMsgQueue;

            receiveEventArgs = new SocketAsyncEventArgs();
            receiveEventArgs.Completed += Receive_Completed;
            sendEventArgs = new SocketAsyncEventArgs();
            sendEventArgs.Completed += Send_Completed;
            connBufferManager.SetBuffer(new byte[ConnConfiguration.RECEIVE_BUFFER_SIZE + ConnConfiguration.SEND_BUFFER_SIZE],0, receiveEventArgs, sendEventArgs);
        }

        #region 连接服务器
        public bool Connect()
        {
            _socket = new Socket(SocketType.Stream,ProtocolType.Tcp);
            try
            {
                _socket.Connect(NetworkConfiguration.SERVER_IP, NetworkConfiguration.SERVER_PORT);
                connInfo.Connect();
                StartReceive();
                StartProcessMsgQueue();
                Console.WriteLine("链接服务器成功");
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("客户端连接服务器失败:"+ex);
                return false;
            }
        }

        /// <summary>
        /// 恢复连接
        /// </summary>
        private bool RestoreConnect()
        {
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _socket.Connect(NetworkConfiguration.SERVER_IP, NetworkConfiguration.SERVER_PORT);
                StartReceive();
                StartProcessMsgQueue();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("客户端连接服务器失败:" + ex.Message);
                return false;
            }
        }
        #endregion

        #region 断开连接

        private void DisConnect(string msg)
        {
            if (_socket != null)
            {
                //禁用接收和发送
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                //移除socket
                _socket = null;

                connInfo.DisConnect("停止使用该链接:" + msg);
                connToken = null;
                msgPack.Init();
                connMsgQueue.Clear();
            }
        }

        private void ReceiveBufferError(string error)
        {
            DisConnect("接收缓冲区错误:"+error);
        }

        private void AccidentalDisConnect(string msg)
        {
            if (connToken != null && _socket != null)
            {
                connToken.AccidentalConnect();
                _socket.Close();
                _socket = null;
                return;
            }
            else
            {
                DisConnect("意外断开但未配置断线重连");
            }

        }

        #endregion

        #region 接收消息
        private void StartReceive()
        {
            //重新设置接收缓冲区
            connBufferManager.SetReceiveBuffer(receiveEventArgs);
            if (_socket == null)
            {
                AccidentalDisConnect("socket无对象" );
                return;
            }
            //异步接收，返回值为true表示此IO操作为挂起状态，false表示此IO操作为同步
            bool willRaiseEvent = _socket.ReceiveAsync(receiveEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(receiveEventArgs);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            //检查远程主机是否断开连接
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                connInfo.AddConnTotalReceiveBytes(e.BytesTransferred);
                //改变缓冲区位置
                connBufferManager.UseReceiveBuffer(e.BytesTransferred);

                //将接收到的数据进行解析
                ProcessReceiveBuffer();
            }
            else
            {
                //断开连接
                AccidentalDisConnect("接收{传输字节:" + e.BytesTransferred + ";操作结果:" + e.SocketError + "}");
            }
        }

        private void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        #endregion

        #region 解析缓存区并将得到的消息包放入接收消息队列
        /// <summary>
        /// 处理缓存区并将得到的消息包放入接收消息队列
        /// </summary>
        private void ProcessReceiveBuffer()
        {
            while (true)
            {
                //若正在解析的消息包剩余长度值未知，则需解析固定头
                if (msgPack.MsgCount == -1)
                {
                    //若缓冲区数据满足固定头长度
                    if (connBufferManager.ValidateReceiveUseSize(msgPack.MsgFixedHeadSize))
                    {
                        //解析固定头
                        //若返回false，缓冲区数据出错，断开链接
                        try
                        {
                            ProcessMsgFixedHead();
                            connBufferManager.FreeReceiveBuffer(msgPack.MsgFixedHeadSize);
                        }
                        catch (Exception ex)
                        {
                            ReceiveBufferError("固定头"+ex.ToString());
                            return;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                //若接收缓冲区已使用大小小于消息包剩余长度，结束解析
                if (!connBufferManager.ValidateReceiveUseSize(msgPack.MsgCount))
                {
                    break;
                }

                //解析消息体
                //返回false,缓冲取数据出错，断开链接
                try
                {
                    ProcessMsgBody();
                    connBufferManager.FreeReceiveBuffer(msgPack.MsgCount);
                    msgPack.Init();
                }
                catch (Exception ex)
                {
                    ReceiveBufferError("消息体" + ex.ToString());
                    return;
                }
            }
            StartReceive();
        }

        /// <summary>
        /// 解析固定头
        /// </summary>
        /// <returns></returns>
        private void ProcessMsgFixedHead()
        {
            ReadOnlySpan<byte> oneSpan, twoSpan;
            connBufferManager.GetReceiveBuffer(msgPack.MsgFixedHeadSize, out oneSpan, out twoSpan);
            //若第二个Span无值
            if (twoSpan == null)
            {
                msgPack.MsgFixedHeadParser(oneSpan);
            }
            else
            {
                msgPack.MsgFixedHeadParser(oneSpan, twoSpan);
            }
        }
        /// <summary>
        ///  解析消息体
        /// </summary>
        /// <returns></returns>
        private void ProcessMsgBody()
        {
            ReadOnlySpan<byte> oneSpan, twoSpan;
            connBufferManager.GetReceiveBuffer(msgPack.MsgCount, out oneSpan, out twoSpan);
            MsgBody msgBody;
            if (twoSpan == null)
            {
                msgBody = msgPack.MsgBodyParser(oneSpan);
            }
            else
            {
                msgBody = msgPack.MsgBodyParser(oneSpan, twoSpan);
            }
            connMsgQueue.EnqueueToWaitProcessesMsgQueue(msgBody);
        }
        #endregion

        #region 处理消息队列
        /// <summary>
        /// 开始处理消息队列
        /// </summary>
        private void StartProcessMsgQueue()
        {
            ThreadPool.QueueUserWorkItem(ProcessMsgQueue);
        }
        /// <summary>
        /// 处理消息队列
        /// </summary>
        private void ProcessMsgQueue(Object state)
        {
            if ((!connMsgQueue.GetWaitProcessesMsgQueueState) && (!connMsgQueue.GetWaitSendMsgQueueState)){
                Thread.Sleep(ConnConfiguration.NO_MSG_THREAD_SLEEP_MIN_TIME);
            }
            MsgBody msgBody;
            while (connMsgQueue.DequeueInWaitProcessesMsgQueue(out msgBody))
            {
                //Console.WriteLine("开始处理消息");
                RequestDictionary.ProcessRequest(msgBody, EnqueueToThisConnWaitSendMsgQueue);
            }
            StartProcessSend(null);
        }
        #endregion

        #region 发送
        private DateTime lastWriteToSendBuffTime;
        /// <summary>
        /// 准备发送
        /// </summary>
        /// <param name="msgPack"></param>
        private void StartProcessSend(MsgPack msgPack)
        {
            //将携带数据放入等待发送队列
            EnqueueToWaitSendMsgQueue(msgPack);
            //判断缓冲区是否有数据
            if (!connBufferManager.ValidateSendUseSize())
            {
                //无数据则尝试从等待消息队列取出首消息
                MsgPack firstPack;
                if (connMsgQueue.PeekInWaitSendMsgQueue(out firstPack))
                {
                    //如果成功取出则记录消息包内时间戳
                    WriteMsgPackToSendBuff(firstPack);
                    //记录写入时间
                    lastWriteToSendBuffTime = firstPack.MsgBody.MsgTimeDateTime;
                    //将数据写入缓冲区成功再移除
                    connMsgQueue.DequeueInWaitSendMsgQueue();
                }
                //无消息则退出
                else
                {
                    StartProcessMsgQueue();
                    return;
                }
            }

            bool readyToSend = false;
            //没有准备好发送则一直循环或者直接退出发送
            //进入此步缓冲区必定有消息
            while (!readyToSend)
            {
                MsgPack firstPack;
                //尝试从等待消息队列获取首消息(不移除)
                if (connMsgQueue.PeekInWaitSendMsgQueue(out firstPack))
                {
                    //判断缓冲区能否写入下一个数据包
                    if (connBufferManager.ValidateSendFreeSize(firstPack.EstimateMsgPackSizeByUTF8))
                    {
                        //写入数据包
                        WriteMsgPackToSendBuff(firstPack);
                        //将数据写入缓冲区成功再移除
                        connMsgQueue.DequeueInWaitSendMsgQueue();

                        //判断上次发送到现在的时间间隔是否超过发送延时
                        if (DetermineSendInterval())
                        {
                            readyToSend = true;
                        }
                        continue;
                    }
                    else
                    {
                        readyToSend = true;
                        continue;
                    }
                }
                //若等待发送消息队列无消息
                else
                {
                    //判断上次发送到现在的时间间隔是否超过发送延时
                    if (DetermineSendInterval())
                    {
                        readyToSend = true;
                        continue;
                    }
                    else
                    {
                        StartProcessMsgQueue();
                        return;
                    }
                }
            }
            StartSendAsync();
        }

        /// <summary>
        /// 将一个消息包写入发送缓冲区
        /// </summary>
        /// <param name="msgPack"></param>
        private void WriteMsgPackToSendBuff(MsgPack msgPack)
        {
            Span<byte> freeSpan;
            connBufferManager.GetSendBufferFreeSpan(msgPack.EstimateMsgPackSizeByUTF8, out freeSpan);
            int useBuffSize = msgPack.WriteMsgPackIntoSpanByUTF8(ref freeSpan);
            //更改缓冲区位置
            connBufferManager.UseSendBuffer(useBuffSize);

            connInfo.AddConnTotalSendMsg();
        }

        /// <summary>
        /// 判断上次发送到现在的时间间隔是否超过发送延时
        /// </summary>
        /// <returns></returns>
        private bool DetermineSendInterval()
        {
            return (DateTime.Now - lastWriteToSendBuffTime).TotalMilliseconds > ConnConfiguration.SEND_MAX_INTERVAL;
        }

        private void StartSendAsync()
        {
            //Console.WriteLine("开始发送");
            connBufferManager.SetSendBuffer(sendEventArgs);
            if (_socket == null)
            {
                AccidentalDisConnect("socket无对象");
                return;
            }
            bool willRaiseEvent = _socket.SendAsync(sendEventArgs);
            if (!willRaiseEvent)
            {
                ProcessSend(sendEventArgs);
            }
        }
        /// <summary>
        /// 将消息放入等待发送队列
        /// </summary>
        public void EnqueueToWaitSendMsgQueue(MsgPack msgPack)
        {
            if (msgPack != null)
            {
                connMsgQueue.EnqueueToWaitSendMsgQueue(msgPack);
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                connInfo.AddConnTotalSendBytes(e.BytesTransferred);

                connBufferManager.FreeSendBuffer();
                //Console.WriteLine("发送成功:"+e.BytesTransferred);
                StartProcessSend(null);
            }
            else
            {
                //断开链接 
                AccidentalDisConnect("发送{传输字节:" + e.BytesTransferred + ";操作结果:" + e.SocketError + "}");
            }
        }

        private void Send_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessSend(e);
        }
        #endregion
    }
}
