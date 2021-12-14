using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TestClinetForServer.Network
{
    class ConnBufferManager
    {
        byte[] buffer;
        /// <summary>
        /// 接收缓冲区位置信息
        /// </summary>
        BufferIndex receiveBufferInfo;
        /// <summary>
        /// 发送缓冲区位置信息
        /// </summary>
        BufferIndex sendBufferInfo;
        /// <summary>
        /// 发送缓冲区锁
        /// </summary>
        //private readonly object sendBufferLock = new object();

        public ConnBufferManager()
        {
            receiveBufferInfo = new BufferIndex();
            sendBufferInfo = new BufferIndex();
        }

        /// <summary>
        /// 为该链接设置收发缓冲区
        /// </summary>
        /// <param name="bufferPool">公用缓冲池</param>
        /// <param name="startIndex">该链接起始缓冲位置</param>
        /// <param name="receiveEventArgs">该链接接收SocketAsyncEventArgs</param>
        /// <param name="sendEventArgs">该链接发送SocketAsyncEventArgs</param>
        public void SetBuffer(byte[] bufferPool, int startIndex, System.Net.Sockets.SocketAsyncEventArgs receiveEventArgs, SocketAsyncEventArgs sendEventArgs)
        {
            buffer = bufferPool;

            receiveBufferInfo.SetInfo(startIndex, Configuration.ConnConfiguration.RECEIVE_BUFFER_SIZE);
            sendBufferInfo.SetInfo(receiveBufferInfo.bufferEndIndex, Configuration.ConnConfiguration.SEND_BUFFER_SIZE);

            receiveEventArgs.SetBuffer(buffer, receiveBufferInfo.freeStartIndex, receiveBufferInfo.bufferSize);
            sendEventArgs.SetBuffer(buffer, sendBufferInfo.useStartIndex, 0);

        }

        #region 接收缓冲区操作

        /// <summary>
        /// 为SocketAsyncEventArgs设置接收缓冲区
        /// </summary>
        /// <param name="receiveEventArgs"></param>
        /// <returns>false表示缓冲区已满,true表示缓冲区设置完成</returns>
        public bool SetReceiveBuffer(SocketAsyncEventArgs receiveEventArgs)
        {
            if (receiveBufferInfo.freeBufferSize > 0)
            {
                if (receiveBufferInfo.freeStartIndex >= receiveBufferInfo.useStartIndex)
                {
                    receiveEventArgs.SetBuffer(receiveBufferInfo.freeStartIndex, receiveBufferInfo.bufferEndIndex - receiveBufferInfo.freeStartIndex);
                }
                else
                {
                    receiveEventArgs.SetBuffer(receiveBufferInfo.freeStartIndex, receiveBufferInfo.useStartIndex - receiveBufferInfo.freeStartIndex);
                }
                return true;
            }
            receiveEventArgs.SetBuffer(receiveBufferInfo.freeStartIndex, 0);
            return false;
        }

        /// <summary>
        /// 异步接收完成后重新设置接收缓冲区位置信息
        /// </summary>
        /// <param name="receiveEventArgs"></param>
        public void UseReceiveBuffer(int useBuffer)
        {
            //Console.WriteLine("接收缓冲区新增:" + useBuffer+";写入起始位置:"+receiveBufferInfo.freeStartIndex+";写入大小:"+useBuffer);
            receiveBufferInfo.UseBuffer(useBuffer);
        }

        /// <summary>
        /// 解析完成后重新设置接收缓冲区位置信息
        /// </summary>
        public void FreeReceiveBuffer(int freeBuffer)
        {
            receiveBufferInfo.FreeBuffer(freeBuffer);
        }

        /// <summary>
        /// 从接收缓冲区中获取指定长度的数据
        /// </summary>
        /// <param name="getSize">需获取的长度</param>
        /// <param name="oneSpan">需获取的数据的内存区域</param>
        /// <param name="twoSpan">若需从头读取，此Span有值</param>
        /// <returns>false表示接受缓冲区可读数据长度不够，true表示读取成功</returns>
        public bool GetReceiveBuffer(int getSize, out ReadOnlySpan<byte> oneSpan, out ReadOnlySpan<byte> twoSpan)
        {
            //Console.WriteLine("从接收缓冲区中获取指定长度的数据,长度:" + getSize+";当前读取起始位置:"+receiveBufferInfo.useStartIndex);
            //如果需获取的长度大于接受缓冲区已接受的长度，则需等待
            if (getSize > receiveBufferInfo.useBufferSize)
            {
                oneSpan = null;
                twoSpan = null;
                return false;
            }
            else
            {
                //若始写入位置大于始读取位置，则始读取至始写入间为可读取数据
                if (receiveBufferInfo.freeStartIndex > receiveBufferInfo.useStartIndex)
                {
                    //Console.WriteLine("始写入位置大于始读取位置，则始读取至始写入间为可读取数据,取出起始位置:" + receiveBufferInfo.useStartIndex);
                    oneSpan = new ReadOnlySpan<byte>(buffer, receiveBufferInfo.useStartIndex, getSize);
                    twoSpan = null;
                }
                //若始写入位置小于等于始读取位置，则可读取数据有两段:
                //1.始读取至接受缓冲区尾 
                //2.接受缓冲区头至发送始写入位置
                else
                {
                    int one = receiveBufferInfo.bufferEndIndex - receiveBufferInfo.useStartIndex;
                    //若范围一大于等于获取长度，则需获取数据均在范围一
                    if (getSize <= one)
                    {
                        //Console.WriteLine("范围一大于等于获取长度，则需获取数据均在范围一,取出起始位置:" + receiveBufferInfo.useStartIndex);
                        oneSpan = new ReadOnlySpan<byte>(buffer, receiveBufferInfo.useStartIndex, getSize);
                        twoSpan = null;
                    }
                    //否则需将范围一数据与范围二数据结合
                    else
                    {
                        int two = getSize - one;
                        oneSpan = new ReadOnlySpan<byte>(buffer, receiveBufferInfo.useStartIndex, one);
                        twoSpan = new ReadOnlySpan<byte>(buffer, receiveBufferInfo.bufferStartIndex, two);
                        //Console.WriteLine("需将范围一数据与范围二数据结合,取出起始位置:" + receiveBufferInfo.useStartIndex + ";范围一长度:" + one + ";还需要:" + two
                        //    + ";oneSpan长度:" + oneSpan.Length + ";TwoSpan长度:" + twoSpan.Length);
                    }
                }
                return true;
            }

        }

        /// <summary>
        /// 验证接收缓冲区已接收数据大小是否足够
        /// </summary>
        /// <param name="size">比较大小</param>
        /// <returns></returns>
        public bool ValidateReceiveUseSize(int size)
        {
            return receiveBufferInfo.useBufferSize >= size;
        }
        /// <summary>
        /// 验证接收缓冲区是否还有空闲空间
        /// </summary>
        /// <returns></returns>
        public bool ValidateReceiveFreeSize()
        {
            return receiveBufferInfo.freeBufferSize > 0;
        }
        #endregion

        #region 发送缓冲区操作
        /// <summary>
        /// 为SocketAsyncEventArgs设置发送缓冲区
        /// </summary>
        /// <param name="sendEventArgs"></param>
        /// <returns></returns>
        public bool SetSendBuffer(SocketAsyncEventArgs sendEventArgs)
        {
            if (sendBufferInfo.useBufferSize > 0)
            {
                sendEventArgs.SetBuffer(sendBufferInfo.useStartIndex, sendBufferInfo.useBufferSize);
                return true;
            }
            sendEventArgs.SetBuffer(sendBufferInfo.useStartIndex, 0);
            return false;
        }

        /// <summary>
        /// 更改发送缓冲区使用范围标识
        /// </summary>
        /// <param name="useBuffer">本次使用大小</param>
        public void UseSendBuffer(int useBuffer)
        {
            //lock (sendBufferLock)
            //{
            //Console.WriteLine("发送缓冲区新增:" + useBuffer + ";已有:" +sendBufferInfo.useBufferSize);
            sendBufferInfo.UseBuffer(useBuffer);
            //}
        }

        /// <summary>
        /// 根据预估的大小取出相应的内存范围
        /// </summary>
        /// <param name="useSize">预估大小</param>
        /// <param name="freeSpan">可使用内存范围</param>
        /// <returns></returns>
        public bool GetSendBufferFreeSpan(int useSize, out Span<byte> freeSpan)
        {
            if (sendBufferInfo.freeBufferSize > useSize)
            {
                freeSpan = new Span<byte>(buffer, sendBufferInfo.freeStartIndex, useSize);
                return true;
            }
            freeSpan = null;
            return false;
        }
        /// <summary>
        /// 释放接收缓冲区
        /// </summary>
        public void FreeSendBuffer()
        {
            sendBufferInfo.InitInfo();
        }

        /// <summary>
        /// 验证发送缓冲区是否还能放入指定大小数据
        /// </summary>
        /// <param name="size">指定大小</param>
        public bool ValidateSendFreeSize(int size)
        {
            return sendBufferInfo.freeBufferSize >= size;
        }
        /// <summary>
        /// 验证发送缓冲区是否还有未发送数据
        /// </summary>
        /// <returns></returns>
        public bool ValidateSendUseSize()
        {
            return sendBufferInfo.useBufferSize > 0;
        }
        #endregion

        struct BufferIndex
        {
            /// <summary>
            ///  缓冲区起始位置
            /// </summary>
            public int bufferStartIndex;
            /// <summary>
            /// 缓冲区终止位置(缓冲区可写入位置不包含此位)
            /// </summary>
            public int bufferEndIndex;
            /// <summary>
            /// 缓冲区大小
            /// </summary>
            public int bufferSize;
            /// <summary>
            /// 空闲缓冲区起始位置\从此处开始写入(指示缓冲区从此位开始为可使用字节段)
            /// </summary>
            public int freeStartIndex;
            /// <summary>
            /// 已使用缓冲区起始位置\从此处开始读取(指示缓冲区从此位开始为已使用字段)
            /// </summary>
            public int useStartIndex;
            /// <summary>
            /// 缓冲区空闲大小
            /// </summary>
            public int freeBufferSize;
            /// <summary>
            /// 缓冲区已使用大小
            /// </summary>
            public int useBufferSize
            {
                get
                {
                    return bufferSize - freeBufferSize;
                }
            }

            public void SetInfo(int bufferStartIndex, int bufferSize)
            {
                this.bufferStartIndex = bufferStartIndex;
                this.bufferSize = bufferSize;
                bufferEndIndex = bufferStartIndex + bufferSize;
                InitInfo();
            }

            public void InitInfo()
            {
                freeStartIndex = bufferStartIndex;
                useStartIndex = bufferStartIndex;
                freeBufferSize = bufferSize;
            }

            public void UseBuffer(int useSize)
            {
                freeStartIndex += useSize;
                if (freeStartIndex >= bufferEndIndex)
                {
                    freeStartIndex = freeStartIndex - bufferEndIndex + bufferStartIndex;
                }
                freeBufferSize -= useSize;
            }

            public void FreeBuffer(int freeSize)
            {
                useStartIndex += freeSize;
                if (useStartIndex >= bufferEndIndex)
                {
                    useStartIndex = useStartIndex - bufferEndIndex + bufferStartIndex;
                }
                freeBufferSize += freeSize;
            }
        }
    }
}
