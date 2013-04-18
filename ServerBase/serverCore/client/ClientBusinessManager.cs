using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.IO;

using ServerBase.database;

using System.Threading;
using ServerBase.Transaction;

namespace ServerBase
{
    class ClientBusinessManager
    {
        /// <summary>
        /// 用户名密码认证，权限分配读取
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="in_message"></param>
        /// <param name="newClientInfo"></param>
        /// <returns></returns>
        public static Boolean Authenticate(NetworkStream dataStream,Message in_message,ClientInfo newClientInfo)
        {
            Message out_message = new Message();
            

            //登陆确认命令
            out_message.Command = Message.CommandHeader.LoginAck;

            //从输入信息中获取用户名、密码、工具类型、工程id
            string name = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0];
            string passwd = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1];
            string tooltype = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[2];
            string prjid = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[3];

            //先进行用户名密码校验 
            Boolean isAuthSucceed = UserBussinessManager.CheckUser(name, passwd);
            if (isAuthSucceed)//消息体需要根据数据库检索结果//同时初始化permission
            {//验证通过
            
                //初始化ClientInfo中name、passwd相应属性
                newClientInfo.name = name;
                newClientInfo.passwd = passwd;
                newClientInfo.projectid = prjid;

                //工程id为空，说明工具刚启动，则校验是否用户是否有访问该工具的权限
                if (prjid.Equals(string.Empty))
                {
                    newClientInfo.permissionList = Database.queryPermissionbyPrjtype(name, tooltype);
                }
                else
                {
                    //初始化ClientInfo中permission相应属性
                    //newClientInfo.permissionList = Database.queryPermission(name);
                    newClientInfo.permissionList = Database.queryPermissionbyPrjID(name, prjid);
                }

                if (newClientInfo.permissionList != null)
                {
                    Console.WriteLine("权限存在");
                    string allowSolutionsAndProjects = "allow";
                    out_message.MessageBody = Encoding.Unicode.GetBytes(allowSolutionsAndProjects);
                 
                    //打包输出信息,将输出信息写入输出流
                    dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                  

                    //确认之后发一个含有服务器所有项目工程名称的xml描述文件
                    //xml描述文件由服务器动态生成
                    /*String mess_xml = GenerateXml();
                    if (mess_xml.CompareTo("") != 0)
                    {
                        out_message.MessageBody = Encoding.Unicode.GetBytes(mess_xml);
                        dataStream.Write(out_message.ToBytes(),0,out_message.MessageLength);
                        return true;
                        //非空
                    }
                    else
                    {
                        out_message.MessageBody = Encoding.Unicode.GetBytes("无工程信息");
                        dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                        return false;
                        //空文件
                    }*/
                    /*
                     * 注释(bhs)
                     */


                    //阻塞读取数据，等待客户端反馈信息
                    while (!dataStream.CanRead)
                    {
                        Thread.Sleep(5);//5ms
                    }

                    while (!dataStream.DataAvailable)
                    {
                        Thread.Sleep(5);//5ms
                    }

                    in_message = Message.Parse(dataStream);
                    string s = Encoding.Unicode.GetString(in_message.MessageBody);
                    byte[] b = new byte[45];

                    //客户端发送ready信息，则向客户端传送服务器的项目列表xml文件
                    if (s.CompareTo("ready") == 0)
                    {
                        //ClientBusinessManager.SendSolutionProjectListXml(dataStream);
                        return true;
                    }
                    else

                        return false;
                }
                else
                {
                    Console.WriteLine("权限为空");

                    string allowSolutionsAndProjects = "deny";
                    out_message.MessageBody = Encoding.Unicode.GetBytes(allowSolutionsAndProjects);
                    Console.WriteLine(Encoding.Unicode.GetString(out_message.MessageBody));

                    //打包输出信息,将输出信息写入输出流
                    dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                    return false;
                }
            }
            else
            {  //验证未通过
                Console.WriteLine("验证未通过");
                out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_DENY_PW);
                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return false;
            }
        }
        public static String GenerateXml()
        {
            return "通过了";
        }
       
       
        /// <summary>
        /// 搜索项目下全部信息
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="in_message"></param>
        /// <param name="newClientInfo"></param>
        /// <returns></returns>
        public static Boolean SearchSolution(NetworkStream dataStream, Message in_message, ClientInfo newClientInfo)
        {
            Message out_message = new Message();

            //登陆确认命令
            out_message.Command = Message.CommandHeader.SearchSolution;

            //从输入信息中获取项目名称
            string name = Encoding.Unicode.GetString(in_message.MessageBody);

            //用户名是否存在 
            Boolean isSolutionExisted = true;// UserExisted(name);
            if (!isSolutionExisted)//消息体需要根据数据库检索结果//同时初始化permission
            {//用户不存在            
                out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_NOTEXISTED);

                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return false;
            }
            else
            {  //用户存在
                List<ProjectInfo> solutions = Database.querySolution(name);
                if (solutions == null)
                {
                    out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_NOTEXISTED);

                    //打包输出信息,将输出信息写入输出流
                    dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                    return false;
                }
                else
                {
                    int projectnum = solutions.Count;
                    string code = projectnum.ToString();
                    List<string> activeclients = ClientThreadManager.GetActiveClients();
                    foreach (ProjectInfo p in solutions)
                    {
                        if(activeclients.Contains(p.ProjectID)) 
                            code += ":" + p.ProjectType + ":" + p.ProjectID.ToString()+":在线"; 
                        else
                            code += ":" + p.ProjectType + ":" + p.ProjectID.ToString() + ":脱机"; 
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
