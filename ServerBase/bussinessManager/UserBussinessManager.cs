using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.Sockets;
using ServerBase.database;

namespace ServerBase
{
    class UserBussinessManager
    {
        /// <summary>
        /// 检查用户名是否存在
        /// </summary>
        /// <param name="name">用户名</param>
        /// <param name="passd">密码</param>
        /// <returns></returns>
        public static Boolean CheckUser(string name, string passd)
        {
            //数据库查询等操作
            User user = Database.queryUser(name);
            if (user != null && user.passwd.CompareTo(passd) == 0)
            {
                Console.WriteLine("name:" + user.name);
                Console.WriteLine("passwd:" + user.passwd);
                Console.WriteLine("groupId:" + user.groupId);
                return true;
            }

            return false;
        }
        /// <summary>
        /// 管理客户端添加用户
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="in_message"></param>
        /// <param name="newClientInfo"></param>
        /// <returns></returns>
        public static Boolean AddUser(NetworkStream dataStream, Message in_message, ClientInfo newClientInfo)
        {
            Message out_message = new Message();

            //登陆确认命令
            out_message.Command = Message.CommandHeader.AddUser;

            //从输入信息中获取用户名、密码
            string name = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0];
            string passwd = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1];
            //int groupid = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[2];
            int groupid = Int32.Parse(Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[2]);


            //用户名密码校验 
            Boolean isUserExisted = UserExisted(name);
            if (!isUserExisted)//消息体需要根据数据库检索结果//同时初始化permission
            {//验证通过

                Database.insertUser(name, passwd, groupid);

                out_message.MessageBody = Encoding.Unicode.GetBytes("succeed");

                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);

                return true;
            }
            else
            {  //验证未通过
                Console.WriteLine("User has existed!");
                out_message.MessageBody = Encoding.Unicode.GetBytes("existed");
                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return false;
            }
        }



        /// <summary>
        /// 搜索用户是否存在
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Boolean UserExisted(string name)
        {
            //数据库查询等操作
            User user = Database.queryUser(name);
            if (user != null)//
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 搜索用户信息
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="in_message"></param>
        /// <param name="newClientInfo"></param>
        /// <returns></returns>
        public static Boolean SearchUser(NetworkStream dataStream, Message in_message, ClientInfo newClientInfo)
        {
            Message out_message = new Message();

            //登陆确认命令
            out_message.Command = Message.CommandHeader.SearchUser;

            //从输入信息中获取用户名
            string name = Encoding.Unicode.GetString(in_message.MessageBody);

            //用户名是否存在 
            Boolean isUserExisted = UserExisted(name);
            if (!isUserExisted)//消息体需要根据数据库检索结果//同时初始化permission
            {//用户不存在            
                out_message.MessageBody = Encoding.Unicode.GetBytes("notexisted");

                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return false;
            }
            else
            {  //用户存在
                User tempuser = Database.queryUser(name);

                out_message.MessageBody = Encoding.Unicode.GetBytes(tempuser.name + ":" + tempuser.passwd + ":" + tempuser.groupId.ToString());
                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return true;
            }
        }
    }
}
