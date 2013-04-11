using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using ServerBase.database;
using ServerBase.Transaction;

namespace ServerBase
{
    class DataTransferBussinessManager
    {
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
            Console.WriteLine("file path is " + solutionProjectDirectory);

            //判断文件夹及存在性
            if (Directory.Exists(solutionProjectDirectory) == true // 文件夹存在性
                && Directory.GetFiles(solutionProjectDirectory).Count() != 0)//客户端需要的xml描述文件存在性
            {
                out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_ALLOW);
                isAck = true;
            }
            else
            {
                Console.WriteLine(projectName + " is not exist.");
                out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_DENY);
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
                    out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_SHARP);//该消息体最终被舍弃 "#"
                    out_message.Command = Message.CommandHeader.SendXml;
                    out_message.MessageFlag = Message.MessageFlagHeader.FileEnd;
                    dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                }

                //读取客户端反馈，以判断客户端是否正确接收文件
                Message in_message = Message.Parse(dataStream);
                if (in_message.Command == Message.CommandHeader.ReceivedXml
                    && Encoding.Unicode.GetString(in_message.MessageBody).CompareTo(Constants.M_YES) == 0)
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

        /// <summary>
        /// 发送工程服务器端项目工程xml描述文件projectNameXml给客户端，整个文件的发送均在此方法内完成
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="solutionName"></param>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public static Boolean SendXml(NetworkStream dataStream, string solutionName, string projectName)
        {
            //string solutionProjectDirectory = solutionName+"\\"+projectName;
            string fileName = string.Empty;
            Projectinfo sourceproject = Database.queryProjectInfo(projectName, solutionName);
            string destinationprojectype = string.Empty;
            switch (sourceproject.projectType)
            {
                case "fta":
                    {
                        destinationprojectype = Constants.P_FMEA;
                        break;
                    }
                case "fmea":
                    {
                        destinationprojectype = Constants.P_FTA;
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
                    out_message.MessageBody = Encoding.Unicode.GetBytes(solutionName + ":" + projectName);//项目名+工程名
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
                    }

                    //文件读取结束
                    out_message = new Message();
                    out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_SHARP);//该消息体最终被舍弃
                    out_message.Command = Message.CommandHeader.SendXml;
                    out_message.MessageFlag = Message.MessageFlagHeader.FileEnd;
                    dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                }

                //读取客户端反馈，以判断客户端是否正确接收文件
                Message in_message = Message.Parse(dataStream);
                if (in_message.Command == Message.CommandHeader.ReceivedXml
                    && Encoding.Unicode.GetString(in_message.MessageBody).CompareTo(Constants.M_YES) == 0)
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
                out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_ALLOW);
                isAck = true;
            }
            else
            {
                out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_DENY);
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
                        projectNameAddTimestampWithRelativePath = solutionName + "\\" + projectName + "\\" + projectName + timestamp + ".xml";
                        if (!File.Exists(projectNameAddTimestampWithRelativePath))
                        {
                            fs = File.Create(projectNameAddTimestampWithRelativePath);
                            fs.Close();//需要关闭该文件，否则下面无法获取文件锁，进行文件读写
                        }
                        break;
                    case Message.MessageFlagHeader.FileMiddle://得到文件内容,采用追加方式写入文件
                        if (!File.Exists(projectNameAddTimestampWithRelativePath))
                        {
                            Console.WriteLine(projectName + " not exist.");
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
                        out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_YES);
                        dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                        XMLtoDB conv = new XMLtoDB(projectNameAddTimestampWithRelativePath);//直接将xml解析并存入数据库

                        //自动推送收到的文件到其它需要的客户端
                        Message pushmessage = new Message();
                        pushmessage.Command = Message.CommandHeader.PushXML;
                        //读取定义的规则，需要扫描数据库或XML
                        //暂时只加入fta与fmea
                        string sourcetype = Database.queryTypeByProjectID(projectName);
                        string targettype = string.Empty;
                        if (sourcetype != null)
                        {
                            switch (sourcetype)
                            {
                                case "fta":
                                    targettype = Constants.P_FTA;
                                    break;
                                case "fmea":
                                    targettype = Constants.P_FMEA;
                                    break;
                            }
                            string destinationprjid = string.Empty;
                            if (!targettype.Equals(string.Empty))
                            {
                                destinationprjid = Database.queryProjectByType(solutionName, targettype).projectID;

                                if (!destinationprjid.Equals(string.Empty))
                                {
                                    pushmessage.MessageBody = Encoding.Unicode.GetBytes(projectName + ":" + destinationprjid + ":" + solutionName);
                                    PushBussinessManager.AutoPushXML(pushmessage);
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
                        out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_NO);
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
                || !File.Exists(solutionProjectDirectory + "\\" + documentName))//客户端需要的当前服务器无法提供
            {
                out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_DENY);
                Console.WriteLine(documentName + " is not exist.");
                isAck = false;
            }
            else
            {
                out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_ALLOW); 
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
        public static Boolean SendDocument(NetworkStream dataStream, string solutionName, string projectName)
        {

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
        public static Boolean SendDocument(NetworkStream dataStream, string solutionName, string projectName, string documentName)
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
                        out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_SHARP);//该消息体最终被舍弃
                        dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);

                        //读取客户端反馈，以判断客户端是否正确接收文件
                        Message in_message = Message.Parse(dataStream);
                        if (in_message.Command == Message.CommandHeader.ReceivedDocument
                            && Encoding.Unicode.GetString(in_message.MessageBody).CompareTo(Constants.M_YES) == 0)
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
                out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_ALLOW);
                isAck = true;
            }
            else
            {
                out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_DENY);
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
                            out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_YES);
                            dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
                            return true;
                        default:
                            //发送接收失败的确认数据包
                            out_message = new Message();
                            out_message.Command = Message.CommandHeader.ReceivedDocument;
                            out_message.MessageBody = Encoding.Unicode.GetBytes(Constants.M_NO);
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
