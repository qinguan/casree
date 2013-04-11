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
            Boolean isAuthSucceed = CheckUser(name, passwd);
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
                        ClientBusinessManager.SendSolutionProjectListXml(dataStream);
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
                out_message.MessageBody = Encoding.Unicode.GetBytes("deny_pw");
                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return false;
            }
        }
        public static String GenerateXml()
        {
            return "通过了";
        }
        //test
        //public static Boolean CheckUser(string name, string passd)
        //{
        //    return true;
        //}
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

                Database.insertUser(name, passwd,groupid );

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
            Boolean isProjectExisted = ProjectExisted(ProjectID,ProgramID);
            if (!isProjectExisted)//消息体需要根据数据库检索结果
            {//验证通过

                Database.insertSolutionProject(ProgramID,ProjectID,ProjectDescription,ProjectType);
               
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
            Boolean isProjectExisted = ProjectExisted(ProjectID,ProgramID);
            if (isProjectExisted==false)//消息体需要根据数据库检索结果//同时初始化permission
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

            Console.WriteLine("dadfadfada"+UserName+ProjectID+ProgramID);
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
                if ((Database.deleteProject(UserName,ProjectID,ProgramID)))
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
        /// 搜索工程是否存在
        /// </summary>
        /// <param name="ProjectID"></param>
        /// <param name="ProgramID"></param>
        /// <returns></returns>
        public static Boolean ProjectExisted(String ProjectID,String ProgramID)
        {
            //数据库查询操作
            List<string> projectlist = Database.querySolutionProject(ProgramID);
            if (projectlist == null)
            {
                Console.WriteLine("project is null");
                return false;
            }
           
            bool yes=false;
            foreach (string s in projectlist)
            {
                if (s.CompareTo(ProjectID) == 0)
                {
                    yes = true;
                }
            }
       
            return yes;
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

                out_message.MessageBody = Encoding.Unicode.GetBytes(tempuser.name+":"+tempuser.passwd+":"+tempuser.groupId.ToString());
                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return true;
            }
        }
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
            Boolean isUserExisted = UserExisted(name);
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
                Boolean isPermissionExisted = PermissionExisted(name,projectid,permissionlevel);
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
        public static Boolean PermissionExisted(string name,string projectid,int permissionlevel)
        {
            //数据库查询等操作
            List<Permission> permission = Database.queryPermission(name);
            if (permission!=null)//
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
                List<Permission> permissions = Database.queryPermission(name);
                if (permissions==null)
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
                        code += ":"+p.projectName + ":" + p.permissionlevel.ToString();
                    }
                    out_message.MessageBody = Encoding.Unicode.GetBytes(code);
                    //打包输出信息,将输出信息写入输出流
                    dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                    return true;
                }
            }
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
                out_message.MessageBody = Encoding.Unicode.GetBytes("notexisted");

                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return false;
            }
            else
            {  //用户存在
                List<ProjectInfo> solutions = Database.querySolution(name);
                if (solutions == null)
                {
                    out_message.MessageBody = Encoding.Unicode.GetBytes("notexisted");

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
        /// <summary>
        /// 针对客户端获取xml文件的请求GetXmlRequest，服务器首先要确认客户端请求的合理性，然后决定是否发xml文件
        /// 判断文件的存在性，判断客户端的权限是否足够
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="in_message"></param>
        /// <returns></returns>
        public static Boolean SendAckToClientGetXmlRequest(NetworkStream dataStream, Message in_message)
        {
            string solutionName = string.Empty;
            string projectName = string.Empty;
            string solutionProjectDirectory = string.Empty;

            Boolean isAck;
            Message out_message = new Message();
            out_message.Command = Message.CommandHeader.GetXmlAck;

            solutionName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0];
            projectName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1];
            
            //默认在当前目录下搜索项目及工程文件夹
            solutionProjectDirectory = solutionName + "\\" + projectName;
            
            
            //还需要添加权限判断
            Console.WriteLine("file path is "+solutionProjectDirectory);

          


            //判断文件夹及存在性
            if (Directory.Exists(solutionProjectDirectory)==true // 文件夹存在性
                && Directory.GetFiles(solutionProjectDirectory).Count() != 0)//客户端需要的xml描述文件存在性
            {
                out_message.MessageBody = Encoding.Unicode.GetBytes("allow"); ;
                isAck = true;  
            }
            else
            {
                Console.WriteLine(projectName + " is not exist.");
                out_message.MessageBody = Encoding.Unicode.GetBytes("deny"); ;
                isAck = false;
            }
            dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);

            return isAck;
        }

        /// <summary>
        /// 权限范围内的发送项目及工程列表，该文件动态生成
        /// </summary>
        /// <param name="dataStream"></param>
        /// <returns></returns>
        public static Boolean SendSolutionProjectListXml(NetworkStream dataStream)
        {

            //暂定于当前程序运行主目录下 待改
            string fileName = "SolutionProjectList";
          
            //打开本地文件，读取内容，分行写到message中,连续发送
            //第一个数据包消息体：文件名
            //最后一个数据包消息体：#
            try
            {
               
                Message out_message;
                using (StreamReader sr = new StreamReader(fileName, Encoding.Default))
                {
                    //传送文件开始的第一个数据包
                    out_message = new Message();
                    out_message.MessageBody = Encoding.Unicode.GetBytes(fileName);
                    out_message.Command = Message.CommandHeader.SendXml;
                    out_message.MessageFlag = Message.MessageFlagHeader.FileBegin;
                    dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);

                   

                    //下面传送文件内容
                    string line = string.Empty;
                    int packetNum = 1;
                    while ((line = sr.ReadLine()) != null)
                    {
                        out_message = new Message();
                        out_message.MessageBody = Encoding.Unicode.GetBytes(line);
                        out_message.Command = Message.CommandHeader.SendXml;
                        out_message.FilePacketNumber = packetNum++;//数据包添加序号
                        out_message.MessageFlag = Message.MessageFlagHeader.FileMiddle;
                        dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                       // Console.WriteLine(line);
                    }

                    //文件读取结束
                    out_message = new Message();
                    out_message.MessageBody = Encoding.Unicode.GetBytes("#");//该消息体最终被舍弃
                    out_message.Command = Message.CommandHeader.SendXml;
                    out_message.MessageFlag = Message.MessageFlagHeader.FileEnd;
                    dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                }

                //读取客户端反馈，以判断客户端是否正确接收文件
                Message in_message = Message.Parse(dataStream);
                if (in_message.Command == Message.CommandHeader.ReceivedXml
                    && Encoding.Unicode.GetString(in_message.MessageBody).CompareTo("yes") == 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendXml: " + ex.Message);
                return false;
            }
            
            return true;
             
        }

        public static void PushXMLAck(NetworkStream dataStream, Message in_message, ClientInfo newClientInfo)
        {
            ClientInfo adminclient = ClientThreadManager.GetClient("ADMIN");
            if (adminclient == null)
            {
                if (in_message.Command == Message.CommandHeader.PushXMLAck)
                    if (Encoding.Unicode.GetString(in_message.MessageBody).CompareTo("yes") == 0)
                    {
                        Console.WriteLine("Pushing successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Pushing failed!");
                    }
            }
            else
            {
                NetworkStream pushackStream = adminclient.client.GetStream();
                Message out_message = new Message();
                out_message.Command = Message.CommandHeader.PushXMLAck;

                if (in_message.Command == Message.CommandHeader.PushXMLAck)
                    if (Encoding.Unicode.GetString(in_message.MessageBody).CompareTo("yes") == 0)
                    {
                        Console.WriteLine("Pushing successfully!");
                        out_message.MessageBody = Encoding.Unicode.GetBytes("succeed");  //发送成功，将消息送回调度者
                        //打包输出信息,将输出信息写入输出流
                        pushackStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                    }
                    else
                    {
                        out_message.MessageBody = Encoding.Unicode.GetBytes("fail");
                        //打包输出信息,将输出信息写入输出流
                        pushackStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                        Console.WriteLine("Pushing failed!");
                    }
            }
        }
        public static Boolean PushXML(NetworkStream dataStream, Message in_message, ClientInfo newClientInfo)
        {
            Console.Write("Start pushing XML from ");
            string fileName = string.Empty;
            Message out_message = new Message();

            //推送命令
            out_message.Command = Message.CommandHeader.PushXML;

            //从输入信息中获取源、目的工程id以及项目id
            string sourceprjid = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0];
            string destinationprjid = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1];
            string solutionname = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[2];
            Console.WriteLine(sourceprjid + " to " + destinationprjid);

            List<ProjectInfo> solutions = Database.querySolution(solutionname); //检测项目是否存在
            if (solutions == null)
            {
                out_message.MessageBody = Encoding.Unicode.GetBytes("notexisted");
                Console.WriteLine("Solution not existed!");
                //打包输出信息,将输出信息写入输出流
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                return false;
            }
            else //项目存在
            {
                string solutionProjectDirectory = solutionname + "\\" + sourceprjid;  //获取源工具数据
                //找一个xml描述文件，暂定为GetFiles函数的第一个文件,有待于数据库协作判断，待加
                //foreach (var file in Directory.GetFiles(solutionProjectDirectory))
                //{ 

                //}
                // fileName = Directory.GetFiles(solutionProjectDirectory).First();
                fileName = Directory.GetFiles(solutionProjectDirectory).Last();

                if (fileName == null)
                {
                    return false;
                }
                Console.WriteLine("File found:" + fileName);
                //打开本地文件，读取内容，分行写到message中,连续发送
                //第一个数据包消息体：文件名
                //最后一个数据包消息体：#
                try
                {
                    ClientInfo destinationclient = ClientThreadManager.GetClient(destinationprjid);
                    if (destinationclient == null)   //检测目标客户端是否在线
                    {
                        out_message.MessageBody = Encoding.Unicode.GetBytes("offline");
                        Console.WriteLine("Destination client is offline!");
                        //打包输出信息,将输出信息写入输出流
                        dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                        return false;
                    }
                    else
                    {
                        Console.WriteLine("Destination client is online! Ready to push......");
                        NetworkStream pushdataStream = destinationclient.client.GetStream();
                        Message push_message;
                        using (StreamReader sr = new StreamReader(fileName, Encoding.Default))
                        {
                            //传送文件开始的第一个数据包
                            push_message = new Message();
                            push_message.MessageBody = Encoding.Unicode.GetBytes(solutionname + ":" + destinationprjid);//项目名+工程名
                            push_message.Command = Message.CommandHeader.PushXML;
                            push_message.MessageFlag = Message.MessageFlagHeader.FileBegin;
                            pushdataStream.Write(push_message.ToBytes(), 0, push_message.MessageLength);
                            //下面传送文件内容
                            string line = string.Empty;
                            int packetNum = 1;
                            while ((line = sr.ReadLine()) != null)
                            {
                                push_message = new Message();
                                push_message.MessageBody = Encoding.Unicode.GetBytes(line);
                                push_message.Command = Message.CommandHeader.PushXML;
                                push_message.FilePacketNumber = packetNum++;//数据包添加序号
                                push_message.MessageFlag = Message.MessageFlagHeader.FileMiddle;
                                pushdataStream.Write(push_message.ToBytes(), 0, push_message.MessageLength);
                            }

                            //文件读取结束
                            push_message = new Message();
                            push_message.MessageBody = Encoding.Unicode.GetBytes("#");//该消息体最终被舍弃
                            push_message.Command = Message.CommandHeader.PushXML;
                            push_message.MessageFlag = Message.MessageFlagHeader.FileEnd;
                            pushdataStream.Write(push_message.ToBytes(), 0, push_message.MessageLength);
                            Console.WriteLine("Sending......Waiting for callback......");
                        }
                        out_message.MessageBody = Encoding.Unicode.GetBytes("waiting");  //发送成功，但是尚未接到客户端回馈，将消息送回调度者
                        //打包输出信息,将输出信息写入输出流
                        dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);

                        //读取客户端反馈，以判断客户端是否正确接收文件
                        //    Message push_back_message = Message.Parse(pushdataStream);

                        //if (push_back_message.Command == Message.CommandHeader.PushXMLAck)
                        //    if (Encoding.Unicode.GetString(push_back_message.MessageBody).CompareTo("yes") == 0)
                        //    {
                        //        Console.WriteLine("Pushing successfully!");
                        //        out_message.MessageBody = Encoding.Unicode.GetBytes("succeed");  //发送成功，将消息送回调度者
                        //        //打包输出信息,将输出信息写入输出流
                        //        dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                        //        return true;
                        //    }
                        //    else
                        //    {
                        //        out_message.MessageBody = Encoding.Unicode.GetBytes("fail");
                        //        //打包输出信息,将输出信息写入输出流
                        //        dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                        //        Console.WriteLine("Pushing failed!");
                        //        return false;
                        //    }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("PushXML: " + ex.Message);
                }
                return false;
            }
        }

        public static Boolean AutoPushXML(Message in_message)
        {
            Console.Write("Auto pushing XML from ");
            string fileName = string.Empty;

            //从输入信息中获取源、目的工程id与项目id 
            string sourceprjid = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0];
            string destinationprjid = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1];
            string solutionname = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[2];
            Console.WriteLine(sourceprjid+" to "+destinationprjid);
                string solutionProjectDirectory = solutionname + "\\" + sourceprjid;  //获取源工具数据
                //找一个xml描述文件，暂定为GetFiles函数的第一个文件,有待于数据库协作判断，待加
                //foreach (var file in Directory.GetFiles(solutionProjectDirectory))
                //{ 

                //}
                // fileName = Directory.GetFiles(solutionProjectDirectory).First();
                fileName = Directory.GetFiles(solutionProjectDirectory).Last();

                if (fileName == null)
                {
                    return false;
                }
                Console.WriteLine("File found:" + fileName);
                //打开本地文件，读取内容，分行写到message中,连续发送
                //第一个数据包消息体：文件名
                //最后一个数据包消息体：#
                try
                {
                    ClientInfo destinationclient= ClientThreadManager.GetClient(destinationprjid);
                    if (destinationclient == null)   //检测目标客户端是否在线
                    {
                        Console.WriteLine("Destination client is offline!");
                        return false;
                    }
                    else
                    {
                        Console.WriteLine("Destination client is online! Ready to push......");
                        NetworkStream pushdataStream = destinationclient.client.GetStream();
                        Message push_message;
                        using (StreamReader sr = new StreamReader(fileName, Encoding.Default))
                        {
                            //传送文件开始的第一个数据包
                            push_message = new Message();
                            push_message.MessageBody = Encoding.Unicode.GetBytes(solutionname + ":" + destinationprjid);//项目名+工程名
                            push_message.Command = Message.CommandHeader.PushXML;
                            push_message.MessageFlag = Message.MessageFlagHeader.FileBegin;
                            pushdataStream.Write(push_message.ToBytes(), 0, push_message.MessageLength);
                            //下面传送文件内容
                            string line = string.Empty;
                            int packetNum = 1;
                            while ((line = sr.ReadLine()) != null)
                            {
                                push_message = new Message();
                                push_message.MessageBody = Encoding.Unicode.GetBytes(line);
                                push_message.Command = Message.CommandHeader.PushXML;
                                push_message.FilePacketNumber = packetNum++;//数据包添加序号
                                push_message.MessageFlag = Message.MessageFlagHeader.FileMiddle;
                                pushdataStream.Write(push_message.ToBytes(), 0, push_message.MessageLength);
                            }

                            //文件读取结束
                            push_message = new Message();
                            push_message.MessageBody = Encoding.Unicode.GetBytes("#");//该消息体最终被舍弃
                            push_message.Command = Message.CommandHeader.PushXML;
                            push_message.MessageFlag = Message.MessageFlagHeader.FileEnd;
                            pushdataStream.Write(push_message.ToBytes(), 0, push_message.MessageLength);
                            Console.WriteLine("Sending......Waiting for callback......");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("PushXML: " + ex.Message);
                }
                return false;
            
        }
        /// <summary>
        /// 发送工程服务器端项目工程xml描述文件projectNameXml给客户端，整个文件的发送均在此方法内完成
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="solutionName"></param>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public static Boolean SendXml(NetworkStream dataStream, string solutionName ,string projectName)
        {
            //string solutionProjectDirectory = solutionName+"\\"+projectName;
            string fileName = string.Empty;
            Projectinfo sourceproject = Database.queryProjectInfo(projectName, solutionName);
            string destinationprojectype = string.Empty;
            switch (sourceproject.projectType)
            {
                case "fta":
                    {
                        destinationprojectype = "fmea";
                        break;
                    }
                case "fmea":
                    {
                        destinationprojectype = "fta";
                        break;
                    }
            }
            //获取数据库中同一项目下对应类型的工程
            Projectinfo project = Database.queryProjectByType(solutionName, destinationprojectype);
            string solutionProjectDirectory = solutionName + "\\" + project.projectID;
            //找一个xml描述文件，暂定为GetFiles函数的第一个文件,有待于数据库协作判断，待加
            //foreach (var file in Directory.GetFiles(solutionProjectDirectory))
            //{ 
                
            //}
           // fileName = Directory.GetFiles(solutionProjectDirectory).First();
            fileName = Directory.GetFiles(solutionProjectDirectory).Last();
         
            if (fileName == null) 
            {
                return false;
            }

            //打开本地文件，读取内容，分行写到message中,连续发送
            //第一个数据包消息体：文件名
            //最后一个数据包消息体：#
            try
            {
                Message out_message;
                using (StreamReader sr = new StreamReader(fileName, Encoding.Default))
                {
                    //传送文件开始的第一个数据包
                    out_message = new Message();
                    out_message.MessageBody = Encoding.Unicode.GetBytes(solutionName+":"+projectName);//项目名+工程名
                    out_message.Command = Message.CommandHeader.SendXml;
                    out_message.MessageFlag = Message.MessageFlagHeader.FileBegin;
                    dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);

                    //下面传送文件内容
                    string line = string.Empty;
                    int packetNum = 1;
                    while ((line = sr.ReadLine()) != null)
                    {
                        out_message = new Message();
                        out_message.MessageBody =Encoding.Unicode.GetBytes(line);
                        out_message.Command = Message.CommandHeader.SendXml;
                        out_message.FilePacketNumber = packetNum++;//数据包添加序号
                        out_message.MessageFlag = Message.MessageFlagHeader.FileMiddle;
                        dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                    }

                    //文件读取结束
                    out_message = new Message();
                    out_message.MessageBody = Encoding.Unicode.GetBytes("#");//该消息体最终被舍弃
                    out_message.Command = Message.CommandHeader.SendXml;
                    out_message.MessageFlag = Message.MessageFlagHeader.FileEnd;
                    dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                }

                //读取客户端反馈，以判断客户端是否正确接收文件
                Message in_message = Message.Parse(dataStream);
                if (in_message.Command == Message.CommandHeader.ReceivedXml
                    && Encoding.Unicode.GetString(in_message.MessageBody).CompareTo("yes") == 0)
                {
                    return true;
                }   
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendXml: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// 客户端发送项目工程XML描述文件给服务器请求，服务器加以确认，并将确认信息发送给客户端
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="in_message"></param>
        /// <returns></returns>
        public static Boolean SendAckToClientSendXmlRequest(NetworkStream dataStream, Message in_message)
        {
            //判断客户端上传项目工程文件的权限

            Boolean isAck;
            Message out_message = new Message();
            out_message.Command = Message.CommandHeader.SendXmlAck;

            if (true)//检查permission
            {
                out_message.MessageBody = Encoding.Unicode.GetBytes("allow");
                isAck = true;
            }
            else
            {
                out_message.MessageBody = Encoding.Unicode.GetBytes("deny");
                isAck = false;
            }
            dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
            return isAck;

        }

        /// <summary>
        /// 服务器接受客户端传来的项目工程xml描述文件
        /// </summary>
        /// <param name="dataStream"></param>
        public static Boolean GetXml(NetworkStream dataStream)
        {
            //整个文件接收工作需要在这全部完成，阻塞方式
            FileStream fs;
            Message in_message;
            Message out_message;
            string solutionName = string.Empty;
            string projectName = string.Empty;
            string solutionProjectDirectory = string.Empty;
            //格式@主目录\\子目录\\工程描述文件
            string projectNameAddTimestampWithRelativePath = string.Empty;

            do
            {
                in_message = Message.Parse(dataStream);
                switch (in_message.MessageFlag)
                {
                    case Message.MessageFlagHeader.FileBegin://得到文件名，新建一个以该文件名命名的文件
                        solutionName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0];
                        projectName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1];

                        //项目工程文件夹
                        solutionProjectDirectory = solutionName + "\\" + projectName;

                        //判断当前目录下是否存在该项目及工程文件夹
                        if (!Directory.Exists(solutionProjectDirectory))
                        {
                            //在当前路径下创建项目文件夹                           
                            Directory.CreateDirectory(solutionProjectDirectory);
                        }

                        //生成时间戳 格式：__2011_12_2_19_48_37
                        string timestamp = "__" + DateTime.Now.ToString().Replace(':', '_').Replace(' ', '_').Replace('/', '_');
                        projectNameAddTimestampWithRelativePath = solutionName + "\\" + projectName + "\\" + projectName + timestamp+".xml";
                        if (!File.Exists(projectNameAddTimestampWithRelativePath))
                        {
                            fs = File.Create(projectNameAddTimestampWithRelativePath);
                            fs.Close();//需要关闭该文件，否则下面无法获取文件锁，进行文件读写
                        }
                        break;
                    case Message.MessageFlagHeader.FileMiddle://得到文件内容,采用追加方式写入文件
                        if (!File.Exists(projectNameAddTimestampWithRelativePath))
                        {
                            Console.WriteLine(projectName + " not exist." );
                        }
                        fs = File.Open(projectNameAddTimestampWithRelativePath, FileMode.Append);
                        StreamWriter sw = new StreamWriter(fs, Encoding.Unicode, in_message.MessageBody.Length);
                        sw.WriteLine(Encoding.Unicode.GetString(in_message.MessageBody));
                        sw.Flush();
                        sw.Close();
                        fs.Close();
                        break;
                    case Message.MessageFlagHeader.FileEnd://文件传送结束,需要判断第一个数据包直接跳到这项的 
                        //发送成功接收的确认数据包
                        out_message = new Message();
                        out_message.Command = Message.CommandHeader.ReceivedXml;
                        out_message.MessageBody = Encoding.Unicode.GetBytes("yes");
                        dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                        XMLtoDB conv = new XMLtoDB(projectNameAddTimestampWithRelativePath);//直接将xml解析并存入数据库

                         //自动推送收到的文件到其它需要的客户端
                        Message pushmessage=new Message();
                        pushmessage.Command=Message.CommandHeader.PushXML;
                        //读取定义的规则，需要扫描数据库或XML
                        //暂时只加入fta与fmea
                        string sourcetype=Database.queryTypeByProjectID(projectName);
                        string targettype = string.Empty;
                        if (sourcetype != null)
                        {
                            switch (sourcetype)
                            {
                                case "fta":
                                    targettype = "fmea";
                                    break;
                                case "fmea":
                                    targettype = "fta";
                                    break;
                            }
                            string destinationprjid=string.Empty;
                            if (!targettype.Equals(string.Empty))
                            {
                                destinationprjid = Database.queryProjectByType(solutionName, targettype).projectID;

                                if (!destinationprjid.Equals(string.Empty))
                                {
                                    pushmessage.MessageBody = Encoding.Unicode.GetBytes(projectName + ":" + destinationprjid + ":" + solutionName);
                                    ClientBusinessManager.AutoPushXML(pushmessage);
                                }
                                else
                                    Console.WriteLine("Auto push:Target project not existed!");
                            }
                            else
                                Console.WriteLine("Auto push:No match type found!");
                        }
                        else
                            Console.WriteLine("Auto push:Undefined project type！");

                        return true;
                    default:
                        //发送接收失败的确认数据包
                        out_message = new Message();
                        out_message.Command = Message.CommandHeader.ReceivedXml;
                        out_message.MessageBody = Encoding.Unicode.GetBytes("no");
                        dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                        Console.WriteLine("The xml file trasfer failed...");
                        //若存在接受不全的文件，删除
                        if (File.Exists(projectNameAddTimestampWithRelativePath))
                        {
                            File.Delete(projectNameAddTimestampWithRelativePath);
                        }
                        break;
                }
            } while (in_message.MessageFlag == Message.MessageFlagHeader.FileBegin ||
                in_message.MessageFlag == Message.MessageFlagHeader.FileMiddle);
            return false;
        }

        /// <summary>
        /// 客户端请求获取任意文档，服务器给予确认
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="in_message"></param>
        /// <returns></returns>
        public static Boolean SendAckToClientGetDocumentRequest(NetworkStream dataStream, Message in_message)
        {
            string solutionName = string.Empty;
            string projectName = string.Empty;
            string documentName = string.Empty;
            string solutionProjectDirectory = string.Empty;

            Boolean isAck;
            Message out_message = new Message();
            out_message.Command = Message.CommandHeader.GetDocumentAck;

            solutionName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0];
            projectName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1];
            documentName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[2];
            solutionProjectDirectory = solutionName + "\\" + projectName;
            //默认在当前目录下搜索项目工程xml描述文件

            //还需要添加权限


            //判断文件存在性
            if (Directory.GetFiles(solutionProjectDirectory).Count() == 0 
                || !File.Exists(solutionProjectDirectory+"\\"+documentName))//客户端需要的当前服务器无法提供
            {
                out_message.MessageBody = Encoding.Unicode.GetBytes("deny");
                Console.WriteLine(documentName + " is not exist.");
                isAck = false;
            }
            else
            {
                out_message.MessageBody = Encoding.Unicode.GetBytes("allow"); ;
                isAck = true;
            }
            dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);

            return isAck;
        }

        /// <summary>
        /// 发送项目工程下所有文件
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="solutionName"></param>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public static Boolean SendDocument(NetworkStream dataStream, string solutionName,string projectName){

            string solutionProjectDirectory = solutionName + "\\" + projectName;
            if (!Directory.Exists(solutionProjectDirectory))
            {
                return false;
            }
            else
            {
                //遍历文件夹发送所有文件
                foreach (var s in Directory.GetFiles(solutionProjectDirectory))
                {
                    SendDocument(dataStream, solutionName, projectName, s);
                }
                return true;
            }
        }


        /// <summary>
        /// 服务器发送任意格式文档给客户端
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="documentName"></param>
        public static Boolean SendDocument(NetworkStream dataStream, string solutionName,string projectName,string documentName)
        {
            string solutionProjectDirectory = solutionName + "\\" + projectName;
            string filenameWithRelativePath = solutionProjectDirectory + "\\" + documentName;

            try
            {
                Message out_message;

                //传送文件开始的第一个数据包
                out_message = new Message();
                out_message.MessageBody = Encoding.Unicode.GetBytes(solutionName + ":" + projectName + ":" + documentName);
                out_message.Command = Message.CommandHeader.SendDocument;
                out_message.MessageFlag = Message.MessageFlagHeader.FileBegin;
                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);

                //传送文件内容
                using (FileStream fs = new FileStream(filenameWithRelativePath, FileMode.Open, FileAccess.Read))
                {
                    BinaryReader br = new BinaryReader(fs);
                    byte[] buffer = new byte[8192];//2^13
                    try
                    {
                        int checksize;
                        do
                        {
                            checksize = br.Read(buffer, 0, 8192);
                            if (checksize > 0)
                            {
                                out_message = new Message();
                                out_message.Command = Message.CommandHeader.SendDocument;
                                out_message.MessageFlag = Message.MessageFlagHeader.FileMiddle;
                                out_message.MessageBody = new byte[checksize];
                                if (checksize < 8192)//最后一个数据包字节数不够
                                {
                                    for (int i = 0; i < checksize; i++)
                                        out_message.MessageBody[i] = buffer[i];
                                }
                                else
                                {
                                    buffer.CopyTo(out_message.MessageBody, 0);
                                }

                                dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                            }
                        } while (checksize > 0);

                        //最后一个数据包
                        out_message = new Message();
                        out_message.Command = Message.CommandHeader.SendDocument;
                        out_message.MessageFlag = Message.MessageFlagHeader.FileEnd;
                        //out_message.MessageBody = new byte[1];
                        out_message.MessageBody = Encoding.Unicode.GetBytes("#");//该消息体最终被舍弃
                        dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);

                        //读取客户端反馈，以判断客户端是否正确接收文件
                        Message in_message = Message.Parse(dataStream);
                        if (in_message.Command == Message.CommandHeader.ReceivedDocument
                            && Encoding.Unicode.GetString(in_message.MessageBody).CompareTo("yes") == 0)
                        {
                            return true;
                        }  
                    }
                    catch (EndOfStreamException ex)
                    {
                        Console.WriteLine("SendDocument: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendDocument: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// 客户端请求发送任意格式文档到服务器，服务器给予确认
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="in_message"></param>
        /// <returns></returns>
        public static Boolean SendAckToClientSendDocumentRequest(NetworkStream dataStream, Message in_message)
        {
            //判断客户端上传项目工程文件的权限

            Boolean isAck;
            Message out_message = new Message();
            out_message.Command = Message.CommandHeader.SendXmlAck;

            if (true)//检查permission
            {
                out_message.MessageBody = Encoding.Unicode.GetBytes("allow");
                isAck = true;
            }
            else
            {
                out_message.MessageBody = Encoding.Unicode.GetBytes("deny");
                isAck = false;
            }
            dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
            return isAck;
        }

        /// <summary>
        /// 服务器接受客户端发送过来的任意格式的文档
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="?"></param>
        public static Boolean GetDocument(NetworkStream dataStream)
        {
            //整个文件接收工作需要在这全部完成，阻塞方式
            try
            {
                FileStream fs;
                Message in_message;
                Message out_message;

                string solutionName = string.Empty;
                string projectName = string.Empty;
                string documentName = string.Empty;
                string documentNameAddTimestampWithRelativePath = string.Empty;

                do
                {
                    in_message = Message.Parse(dataStream);
                    switch (in_message.MessageFlag)
                    {
                        case Message.MessageFlagHeader.FileBegin://得到文件名，新建一个以该文件名命名的文件
                            solutionName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0];
                            projectName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1];
                            documentName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[2];

                            //判断当前目录下是否存在该项目及工程文件夹
                            if (!Directory.Exists(solutionName + "\\" + projectName))
                            {
                                //在当前路径下创建项目文件夹                           
                                Directory.CreateDirectory(solutionName + "\\" + projectName);
                            }
                            //生成时间戳 格式：__2011_12_2_19_48_37
                            string timestamp = "__" + DateTime.Now.ToString().Replace(':', '_').Replace(' ', '_').Replace('/', '_');
                            documentNameAddTimestampWithRelativePath = solutionName + "\\" + projectName + "\\" + documentName + timestamp;
                            
                            if (!File.Exists(documentNameAddTimestampWithRelativePath))
                            {
                                fs = File.Create(documentNameAddTimestampWithRelativePath);
                                fs.Close();
                            }
                            break;
                        case Message.MessageFlagHeader.FileMiddle:
                            if (!File.Exists(documentNameAddTimestampWithRelativePath))
                            {
                                Console.WriteLine(documentName + " not exist.");
                            }
                            using (fs = File.Open(documentNameAddTimestampWithRelativePath, FileMode.Append))
                            {
                                BinaryWriter bw = new BinaryWriter(fs);
                                bw.Write(in_message.MessageBody, 0, in_message.MessageBody.Length);
                                bw.Close();
                                fs.Close();
                            }
                            break;
                        case Message.MessageFlagHeader.FileEnd:
                            //发送成功接收的确认数据包
                            out_message = new Message();
                            out_message.Command = Message.CommandHeader.ReceivedDocument;
                            out_message.MessageBody = Encoding.Unicode.GetBytes("yes");
                            dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                            return true;
                        default:
                            //发送接收失败的确认数据包
                            out_message = new Message();
                            out_message.Command = Message.CommandHeader.ReceivedDocument;
                            out_message.MessageBody = Encoding.Unicode.GetBytes("no");
                            dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                            Console.WriteLine("The document file trasfer failed...");
                            //若存在接受不全的文件，删除
                            if (File.Exists(documentNameAddTimestampWithRelativePath))
                            {
                                File.Delete(documentNameAddTimestampWithRelativePath);
                            }
                            break;
                    }
                } while (in_message.MessageFlag == Message.MessageFlagHeader.FileBegin
                    || in_message.MessageFlag == Message.MessageFlagHeader.FileMiddle);

            }
            catch (Exception ex)
            {
                Console.WriteLine("GetDocument: " + ex.Message);
            }

            return false;
        }

      

    }
}
