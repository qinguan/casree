using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.Sockets;
using ServerBase.database;
using System.IO;

namespace ServerBase
{
    class PushBussinessManager
    {
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
            Console.WriteLine(sourceprjid + " to " + destinationprjid);
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
    }
}
