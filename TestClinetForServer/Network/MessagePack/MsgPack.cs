using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestClinetForServer.Configuration;
using TestClinetForServer.Tools;

namespace TestClinetForServer.Network.MessagePack
{
    class MsgPack
    {
        #region 消息包构成
        private MsgFixedHead msgFixedHead;
        private MsgBody msgBody;
        private MsgFixedFoot msgFixedFoot;
        #endregion

        #region 解析
        /// <summary>
        /// 消息包最小大小
        /// 指示当接收消息累积到多大大小时可以开始解析
        /// </summary>
        public readonly int MsgPackMinSize = 22;
        /// <summary>
        /// 消息包固定头大小
        /// </summary>
        public readonly int MsgFixedHeadSize = MsgFixedHead.MsgFixedHeadSize;
        /// <summary>
        /// 指示该消息包除固定头外的剩余长度
        /// 该量在执行完成固定头解析后有值，否则为-1
        /// </summary>
        public int MsgCount { get => msgFixedHead.msgCount; }

        public MsgPack()
        {
            msgFixedHead = new MsgFixedHead();
            msgFixedFoot = new MsgFixedFoot();

            Init();
        }

        public void Init()
        {
            msgFixedHead.Init();
        }

        /// <summary>
        ///  消息固定头解析函数
        ///  在解析前需使用ValidateLength验证数据长度是否不少于消息包最小长度
        /// </summary>
        /// <param name="spanDate"></param>
        /// <returns></returns>
        public void MsgFixedHeadParser(ReadOnlySpan<byte> spanDate)
        {
            if (msgFixedHead.msgCount != -1)
            {
                return;
            }

            //验证首字节是否为标识符，注：单个Span长度至少长度为1
            if (spanDate[0] != MsgConfiguration.MSG_START_TAG)
            {
                throw new ApplicationException("消息包首标识符错误,收到标识符" + spanDate[0] + ";验证标识符为:" + MsgConfiguration.MSG_START_TAG);
            }

            msgFixedHead.msgCount = ConvertTypeTool.ByteArrayToInt32(spanDate.Slice(1));
        }
        /// <summary>
        /// 消息固定头解析函数
        /// 在解析前应先验证可解析数据长度是否不少于消息包最小长度
        /// </summary>
        /// <param name="oneSpan"></param>
        /// <param name="twoSpan"></param>
        /// <param name="msgBody"></param>
        /// <returns></returns>
        public void MsgFixedHeadParser(ReadOnlySpan<byte> oneSpan, ReadOnlySpan<byte> twoSpan)
        {
            //若固定头长度不为-1，则已解析了长度信息
            if (msgFixedHead.msgCount != -1)
            {
                return;
            }

            //注：由于在使用此函数前已经进行了数据长度验证，故两Span长度和必然不小于22
            int receiveDataOneSize = oneSpan.Length;
            //验证首字节是否为标识符，注：单个Span长度至少长度为1
            if (oneSpan[0] != MsgConfiguration.MSG_START_TAG)
            {
                throw new ApplicationException("消息包首标识符错误,收到标识符"+ oneSpan[0]+";验证标识符为:"+ MsgConfiguration.MSG_START_TAG);
            }

            //oneSpan长度可能性：1，2，3，4，>4
            switch (receiveDataOneSize)
            {
                ///oneSpan只有一个字符，则表示剩余长度符均在twoSpan
                case 1: msgFixedHead.msgCount = ConvertTypeTool.ByteArrayToInt32(twoSpan[0], twoSpan[1], twoSpan[2], twoSpan[3]); break;
                case 2: msgFixedHead.msgCount = ConvertTypeTool.ByteArrayToInt32(oneSpan[1], twoSpan[0], twoSpan[1], twoSpan[2]); break;
                case 3: msgFixedHead.msgCount = ConvertTypeTool.ByteArrayToInt32(oneSpan[1], oneSpan[2], twoSpan[0], twoSpan[1]); break;
                case 4: msgFixedHead.msgCount = ConvertTypeTool.ByteArrayToInt32(oneSpan[1], oneSpan[2], oneSpan[3], twoSpan[0]); break;
                default: msgFixedHead.msgCount = ConvertTypeTool.ByteArrayToInt32(oneSpan[1], oneSpan[2], oneSpan[3], oneSpan[4]); break;
            }
        }

        /// <summary>
        /// 消息体解析函数
        /// 传入的span应只包含剩余长度消息
        /// 在解析前应先验证可解析数据长度是否不少于该消息包剩余长度
        /// </summary>
        /// <param name="spanDate"></param>
        /// <param name="msgBody"></param>
        /// <returns></returns>
        public MsgBody MsgBodyParser(ReadOnlySpan<byte> spanDate)
        {
            MsgBody msgBody;
            if (spanDate[spanDate.Length - 1] != MsgConfiguration.MSG_END_TAG)
            {
                throw new ApplicationException("消息包尾标识符错误");
            }
            msgBody = new MsgBody();

            msgBody.MsgBodyParser(spanDate.Slice(0, spanDate.Length - 1));

            return msgBody;
        }

        /// <summary>
        /// 消息体解析函数
        /// 传入的span应只包含剩余长度消息
        /// 在解析前应先验证可解析数据长度是否不少于该消息包剩余长度
        /// </summary>
        /// <param name="oneSpan"></param>
        /// <param name="twoSpan"></param>
        /// <returns></returns>
        public MsgBody MsgBodyParser(ReadOnlySpan<byte> oneSpan, ReadOnlySpan<byte> twoSpan)
        {
            MsgBody msgBody;
            if (twoSpan[twoSpan.Length - 1] != MsgConfiguration.MSG_END_TAG)
            {
                throw new ApplicationException("消息包尾标识符错误");
            }
            msgBody = new MsgBody();
            //若twoSpan只含一个值，该值为尾标识符
            if (twoSpan.Length == 1)
            {
                msgBody.MsgBodyParser(oneSpan.Slice(0, oneSpan.Length));
            }
            else
            {
                msgBody.MsgBodyParser(oneSpan, twoSpan.Slice(0, twoSpan.Length - 1));
            }
            return msgBody;
        }

        #endregion

        #region 装包
        public MsgBody MsgBody { get => msgBody; }

        public MsgPack(MsgBody msgBody)
        {
            this.msgBody = msgBody;
        }

        public int WriteMsgPackIntoSpanByUTF8(ref Span<byte> freeSpan)
        {
            msgFixedHead.msgCount = msgBody.WriteMsgBodyToSpanByUTF8(ref freeSpan, MsgFixedHead.MsgFixedHeadSize)+ MsgFixedFoot.MsgFixedFootSize;
            msgFixedHead.WriteMsgFixedHeadToSpan(ref freeSpan, 0);
            msgFixedFoot.WriteMsgFixedFootToSpan(ref freeSpan, msgFixedHead.msgCount + MsgFixedHead.MsgFixedHeadSize-MsgFixedFoot.MsgFixedFootSize);
            return MsgFixedHead.MsgFixedHeadSize + msgFixedHead.msgCount;
        }

        public int EstimateMsgPackSizeByUTF8 { get => msgBody.EstimateMsgBodySizeByUTF8 + MsgFixedHead.MsgFixedHeadSize + MsgFixedFoot.MsgFixedFootSize; }
        public int EstimateMsgPackSizeByUTF32 { get => msgBody.EstimateMsgBodySizeByUTF32 + MsgFixedHead.MsgFixedHeadSize + MsgFixedFoot.MsgFixedFootSize; }
        #endregion

        struct MsgFixedHead
        {
            public byte msgStartTag { get => MsgConfiguration.MSG_START_TAG; }
            public int msgCount;

            public void Init()
            {
                msgCount = -1;
            }

            public void WriteMsgFixedHeadToSpan(ref Span<byte> freeSpan, int offset)
            {
                freeSpan[0] = msgStartTag;
                ConvertTypeTool.Int32ToSpan(msgCount, ref freeSpan, 1);
            }

            public static readonly int MsgFixedHeadSize = 5;
        }

        struct MsgFixedFoot
        {
            public byte msgEndTag { get => MsgConfiguration.MSG_END_TAG; }

            public void WriteMsgFixedFootToSpan(ref Span<byte> freeSpan, int offset)
            {
                freeSpan[offset] = msgEndTag;
            }

            public static readonly int MsgFixedFootSize = 1;
        }
    }
}
