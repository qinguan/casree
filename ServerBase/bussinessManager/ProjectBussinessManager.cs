using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.Sockets;
using ServerBase.database;

namespace ServerBase
{
    class ProjectBussinessManager
    {
        /// <summary>
        /// 管理客户端添加工程
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="in_message"></param>
        /// <param name="newClientInfo"></param>
        /// <returns></returns>
        public static Boolean AddProject(NetworkStream dataStream, Message in_message, ClientInfo newClientInfo)
        {
            Message out_message = new Message();

            //登陆确认命令
            out_message.Command = Message.CommandHeader.AddProject;

            //从输入信息中获取用ProjectID,ProgramID,ProjectDescription,ProjectType
            string ProjectID = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0];
            string ProgramID = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1];
            string ProjectDescription = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[2];
            //int ProjectType = Int32.Parse(Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[3]);
            string ProjectType = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[3];


            //工程校验 
            Boolean isProjectExisted = ProjectExisted(ProjectID, ProgramID);
            if (!isProjectExisted)//消息体需要根据数据库检索结果
            {//验证通过

                Database.insertSolutionProject(ProgramID, ProjectID, ProjectDescription, ProjectType);

                out_message.MessageBody = Encoding.Unicode.GetBytes("succeed");

                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);

                return true;
            }
            else
            {  //验证未通过
                Console.WriteLine("project has existed!");
                out_message.MessageBody = Encoding.Unicode.GetBytes("existed");
                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return false;
            }
        }
        /// <summary>
        /// 搜索工程信息
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="in_message">工程与项目名称</param>
        /// <param name="newClientInfo"></param>
        /// <returns></returns>
        public static Boolean SearchProject(NetworkStream dataStream, Message in_message, ClientInfo newClientInfo)
        {
            Message out_message = new Message();

            //登陆确认命令
            out_message.Command = Message.CommandHeader.SearchProject;

            //从输入信息中获取工程名和项目名
            string ProjectID = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0];
            string ProgramID = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1];

            //工程是否存在 
            Boolean isProjectExisted = ProjectExisted(ProjectID, ProgramID);
            if (isProjectExisted == false)//消息体需要根据数据库检索结果//同时初始化permission
            {//用户不存在            
                out_message.MessageBody = Encoding.Unicode.GetBytes("notexisted");

                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return false;
            }
            else
            {  //工程存在
                Projectinfo tempproject = Database.queryProjectInfo(ProjectID, ProgramID);

                if (tempproject == null)
                {
                    out_message.MessageBody = Encoding.Unicode.GetBytes("null" + ":" + "null" + ":" + "null" + ":" + "-100");
                    return false;
                }

                out_message.MessageBody = Encoding.Unicode.GetBytes(tempproject.projectID + ":" + tempproject.programID + ":" + tempproject.projectDescription + ":" + tempproject.projectType.ToString());
                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return true;
            }

        }
        /// <summary>
        /// 删除工程
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="in_message"></param>
        /// <param name="newClientInfo"></param>
        /// <returns></returns>
        public static Boolean deleteProject(NetworkStream dataStream, Message in_message, ClientInfo newClientInfo)
        {
            Message out_message = new Message();

            //登陆删除命令
            out_message.Command = Message.CommandHeader.DeleteProject;

            Console.WriteLine("执行删除工程aaaa");

            //从输入信息中获取工程名和项目名
            string UserName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0];
            string ProjectID = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1];
            string ProgramID = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[2];

            Console.WriteLine("dadfadfada" + UserName + ProjectID + ProgramID);
            //权限检测
            if (true)
            { }
            else
            { }
            //工程是否存在 
            Boolean isProjectExisted = ProjectExisted(ProjectID, ProgramID);

            Console.WriteLine(isProjectExisted);
            if (isProjectExisted == false)//消息体需要根据数据库检索结果
            {//用户不存在            
                out_message.MessageBody = Encoding.Unicode.GetBytes("false");

                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return false;
            }
            else
            {  //工程存在
                if ((Database.deleteProject(UserName, ProjectID, ProgramID)))
                {

                    out_message.MessageBody = Encoding.Unicode.GetBytes("succeed");
                    //打包输出信息,将输出信息写入输出流
                    dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 搜索工程是否存在
        /// </summary>
        /// <param name="ProjectID"></param>
        /// <param name="ProgramID"></param>
        /// <returns></returns>
        public static Boolean ProjectExisted(String ProjectID, String ProgramID)
        {
            //数据库查询操作
            List<string> projectlist = Database.querySolutionProject(ProgramID);
            if (projectlist == null)
            {
                Console.WriteLine("project is null");
                return false;
            }

            bool yes = false;
            foreach (string s in projectlist)
            {
                if (s.CompareTo(ProjectID) == 0)
                {
                    yes = true;
                }
            }

            return yes;
        }
    }
}
