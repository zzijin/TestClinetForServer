using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestClinetForServer.Tools;

namespace TestClinetForServer.Network.MessagePack
{
    class MsgBody
    {
        int msgType;
        long msgTime;
        int msgFlag;

        public int MsgType { get => msgType; }
        public long MsgTime { get => msgTime; }
        public int MsgFlag { get => msgFlag; }

        #region 解析
        JObject msgData;
        public JObject MsgData { get => msgData; }

        public MsgBody()
        {

        }

        /// <summary>
        /// 解析消息体数据
        /// </summary>
        /// <param name="spanDate">仅包含消息体的数据</param>
        public void MsgBodyParser(ReadOnlySpan<byte> spanDate)
        {
            msgType = ConvertTypeTool.ByteArrayToInt32(spanDate[0], spanDate[1], spanDate[2], spanDate[3]);
            msgTime = ConvertTypeTool.ByteArrayToLong(spanDate.Slice(4, 8));
            msgFlag = ConvertTypeTool.ByteArrayToInt32(spanDate.Slice(12, 4));
            string msgDataString;

            try
            {
                unsafe
                {
                    fixed (byte* start = &spanDate[16])
                    {
                        msgDataString = Encoding.UTF8.GetString(start, spanDate.Length - 16);
                    }
                }
                msgData = (JObject)JsonConvert.DeserializeObject(msgDataString);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 解析消息体数据,需换行时
        /// </summary>
        /// <param name="oneSpan"></param>
        /// <param name="twoSpan"></param>
        /// <returns></returns>
        public void MsgBodyParser(ReadOnlySpan<byte> oneSpan, ReadOnlySpan<byte> twoSpan)
        {
            int oneSpanSize = oneSpan.Length;
            int twoSpanSize = twoSpan.Length;
            int msgBodySize = oneSpanSize + twoSpanSize;
            string dataString;
            Console.WriteLine("分步解析:oneSpanSize:" + oneSpanSize + "twoSpanSize:" + twoSpanSize);
            if (oneSpanSize < 4)
            {
                int overlenght = 4 - oneSpanSize;
                msgType = ConvertTypeTool.ByteArrayToInt32(oneSpan.Slice(0, oneSpanSize), twoSpan.Slice(0, overlenght));
                msgTime = ConvertTypeTool.ByteArrayToLong(twoSpan.Slice(overlenght, 8));
                overlenght += 8;
                msgFlag = ConvertTypeTool.ByteArrayToInt32(twoSpan.Slice(overlenght, 4));
                overlenght += 4;
                try
                {
                    unsafe
                    {
                        fixed (byte* start = &twoSpan[overlenght])
                        {
                            dataString = Encoding.UTF8.GetString(start, twoSpanSize - overlenght);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else if (oneSpanSize < 12)
            {
                int overlenght = 12 - oneSpanSize;
                msgType = ConvertTypeTool.ByteArrayToInt32(oneSpan.Slice(0, 4));
                msgTime = ConvertTypeTool.ByteArrayToLong(oneSpan.Slice(4, oneSpanSize - 4), twoSpan.Slice(0, overlenght));
                msgFlag = ConvertTypeTool.ByteArrayToInt32(twoSpan.Slice(overlenght, 4));
                overlenght += 4;
                try
                {
                    unsafe
                    {
                        fixed (byte* start = &twoSpan[overlenght])
                        {
                            dataString = Encoding.UTF8.GetString(start, twoSpanSize - overlenght);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else if (oneSpanSize < 16)
            {
                int overlenght = 16 - oneSpanSize;
                msgType = ConvertTypeTool.ByteArrayToInt32(oneSpan.Slice(0, 4));
                msgTime = ConvertTypeTool.ByteArrayToLong(oneSpan.Slice(4, 8));
                msgFlag = ConvertTypeTool.ByteArrayToInt32(oneSpan.Slice(12, oneSpanSize - 12), twoSpan.Slice(0, overlenght));
                try
                {
                    unsafe
                    {
                        fixed (byte* start = &twoSpan[overlenght])
                        {
                            dataString = Encoding.UTF8.GetString(start, twoSpanSize - overlenght);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else
            {
                msgType = ConvertTypeTool.ByteArrayToInt32(oneSpan.Slice(0, 4));
                msgTime = ConvertTypeTool.ByteArrayToLong(oneSpan.Slice(4, 8));
                msgFlag = ConvertTypeTool.ByteArrayToInt32(oneSpan.Slice(12, 4));
                //将属于msgData的部分数据存入一个新byte数组再转换
                //此处不采用分步转码:经测试，分步转码只在执行次数量大时(循环百万次)才有明显优势，该部分执行几率较低，且分步转化代码复杂，不易更改
                int overlenght = oneSpanSize - 16;
                int msgDataSize = msgBodySize - 16;
                byte[] msgData = new byte[msgDataSize];
                try
                {
                    Buffer.BlockCopy(oneSpan.ToArray(), 16, msgData, 0, overlenght);
                    Buffer.BlockCopy(twoSpan.ToArray(), 0, msgData, overlenght, twoSpanSize);
                    dataString = Encoding.UTF8.GetString(msgData);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            Console.WriteLine("分步解析,获取Json:"+dataString);
            try
            {
                msgData = (JObject)JsonConvert.DeserializeObject(dataString);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region 装包

        string msgDataString;
        DateTime msgTimeDateTime;
        public DateTime MsgTimeDateTime { get => msgTimeDateTime; }

        public MsgBody(int msgType, int msgFlag, JObject msgData)
        {
            this.msgType = msgType;
            msgTimeDateTime = DateTime.Now;
            this.msgFlag = msgFlag;
            this.msgDataString = msgData.ToString();
        }

        public int EstimateMsgBodySizeByUTF8 { get => 16 + msgDataString.Length; }
        public int EstimateMsgBodySizeByUTF32 { get => 16 + (msgDataString.Length * 4); }

        /// <summary>
        /// 将消息体转码为byte
        /// </summary>
        /// <param name="freeSpan"></param>
        /// <param name="offset"></param>
        /// <returns>总占用大小</returns>
        public int WriteMsgBodyToSpanByUTF8(ref Span<byte> freeSpan, int offset)
        {
            int writeSize = 0;
            ConvertTypeTool.Int32ToSpan(msgType, ref freeSpan, offset);
            offset += 4;
            ConvertTypeTool.LongToSpan(TimeTool.TimeToTimestamp(msgTimeDateTime), ref freeSpan, offset);
            offset += 8;
            ConvertTypeTool.Int32ToSpan(msgFlag, ref freeSpan, offset);
            offset += 4;

            writeSize += 16;

            unsafe
            {
                fixed (byte* spanStart = &freeSpan[offset])
                {
                    fixed (char* stringStart = msgDataString)
                    {
                        writeSize += Encoding.UTF8.GetBytes(stringStart, msgDataString.Length, spanStart, msgDataString.Length);
                    }
                }
            }
            return writeSize;
        }

        public int WriteMsgBodyToSpanByUTF32(ref Span<byte> freeSpan, int offset)
        {
            int writeSize = 0;
            ConvertTypeTool.Int32ToSpan(msgType, ref freeSpan, offset);
            offset += 4;
            ConvertTypeTool.LongToSpan(MsgTime, ref freeSpan, offset);
            offset += 8;
            ConvertTypeTool.Int32ToSpan(msgFlag, ref freeSpan, offset);
            offset += 4;

            writeSize += 16;

            unsafe
            {
                fixed (byte* spanStart = &freeSpan[offset])
                {
                    fixed (char* stringStart = msgDataString)
                    {
                        writeSize += Encoding.UTF32.GetBytes(stringStart, msgDataString.Length, spanStart, msgDataString.Length * 2);
                    }
                }
            }
            return writeSize;
        }
        #endregion
}
}
