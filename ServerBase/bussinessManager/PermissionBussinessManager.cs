using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.Sockets;
using ServerBase.database;

namespace ServerBase
{
    class PermissionBussinessManager
    {
        /// <summary>
        /// 添加权限
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="in_message"></param>
        /// <param name="newClientInfo"></param>
        /// <returns></returns>
        public static Boolean AddPermission(NetworkStream dataStream, Message in_message, ClientInfo newClientInfo)
        {
            Message out_message = new Message();

            //登陆确认命令
            out_message.Command = Message.CommandHeader.AddPermission;

            //从输入信息中获取用户名、密码
            string name = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0];
            string projectid = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1];
            int permissionlevel = Convert.ToInt32(Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[2]);

            //用户名密码校验 
            Boolean isUserExisted = UserBussinessManager.UserExisted(name);
            if (!isUserExisted)//消息体需要根据数据库检索结果//同时初始化permission
            {//用户不存在
                Console.WriteLine("添加权限：User doesn't existed!");
                out_message.MessageBody = Encoding.Unicode.GetBytes("notexisted");
                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return false;
            }
            else
            {
                Boolean isPermissionExisted = PermissionExisted(name, projectid, permissionlevel);
                if (isPermissionExisted)
                {//权限已经存在
                    out_message.MessageBody = Encoding.Unicode.GetBytes("existed");
                    Console.WriteLine("添加权限：权限已经存在!");
                    //打包输出信息,将输出信息写入输出流
                    dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                    return false;
                }
                else
                {
                    Console.WriteLine("添加权限：准备添加权限");
                    if (Database.insertPermission(name, projectid, permissionlevel))
                    {
                        out_message.MessageBody = Encoding.Unicode.GetBytes("succeed");
                        Console.WriteLine("添加权限：" + name + " " + projectid + " " + permissionlevel.ToString() + "权限添加成功!");
                        //打包输出信息,将输出信息写入输出流
                        dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);

                        return true;
                    }
                    return false;
                }
            }
        }
        /// <summary>
        /// 查找用户是否有对于某工程的权限
        /// </summary>
        /// <param name="name"></param>
        /// <param name="projectid"></param>
        /// <param name="permissionlevel"></param>
        /// <returns></returns>
        public static Boolean PermissionExisted(string name, string projectid, int permissionlevel)
        {
            //数据库查询等操作
            List<Permission> permission = Database.queryPermission(name);
            if (permission != null)//
            {
                foreach (Permission p in permission)
                {
                    if (p.projectName.Equals(projectid) && p.permissionlevel == permissionlevel)
                        return true;
                }
                return false;
            }
            return false;
        }

        public static Boolean SearchPermission(NetworkStream dataStream, Message in_message, ClientInfo newClientInfo)
        {
            Message out_message = new Message();

            //登陆确认命令
            out_message.Command = Message.CommandHeader.SearchPermission;

            //从输入信息中获取用户名
            string name = Encoding.Unicode.GetString(in_message.MessageBody);

            //用户名是否存在 
            Boolean isUserExisted = UserBussinessManager.UserExisted(name);
            if (!isUserExisted)//消息体需要根据数据库检索结果//同时初始化permission
            {//用户不存在            
                out_message.MessageBody = Encoding.Unicode.GetBytes("notexisted");

                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return false;
            }
            else
            {  //用户存在
                List<Permission> permissions = Database.queryPermission(name);
                if (permissions == null)
                {
                    out_message.MessageBody = Encoding.Unicode.GetBytes("notexisted");

                    //打包输出信息,将输出信息写入输出流
                    dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                    return false;
                }
                else
                {
                    int permissionnum = permissions.Count;
                    string code = permissionnum.ToString();
                    foreach (Permission p in permissions)
                    {
                        code += ":" + p.projectName + ":" + p.permissionlevel.ToString();
                    }
                    out_message.MessageBody = Encoding.Unicode.GetBytes(code);
                    //打包输出信息,将输出信息写入输出流
                    dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                    return true;
                }
            }
        }
    }
}
