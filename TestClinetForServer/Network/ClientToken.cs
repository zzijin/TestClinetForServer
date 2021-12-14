using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClinetForServer.Network
{
    class ClientToken
    {
        /// <summary>
        /// 用于查询的客户端唯一ID
        /// </summary>
        byte[] openID;
        /// <summary>
        /// 用于核对客户端的验证密钥
        /// </summary>
        byte[] clientKey;
        /// <summary>
        /// 用于标识链接是否断开
        /// </summary>
        bool connConnectState;
        /// <summary>
        /// 记录意外断开链接时间
        /// </summary>
        DateTime errorDisConnectTime;

        private void RestoreConnect()
        {
            connConnectState = true;
        }

        public void AccidentalConnect()
        {
            connConnectState = false;
            errorDisConnectTime = DateTime.Now;
        }

        /// <summary>
        /// 生成唯一标识
        /// </summary>
        /// <returns></returns>
        public byte[] GenerateOpenID()
        {
            return openID;
        }

        /// <summary>
        /// 获取密钥
        /// </summary>
        /// <param name="openID"></param>
        /// <returns>生成的密钥</returns>
        public void GetClientKey(byte[] clientKey)
        {
            this.clientKey = clientKey;
        }
    }
}
