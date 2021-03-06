﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using ServerBase.database;

namespace ServerBase
{      
    class ServerBase
    {
        public TcpClient client;//客户端实例
        private NetworkStream dataStream;//数据流
        public Thread clientThread;
        public Permission permission = new Permission();//客户端用户权限
        public ClientInfo clientInfo;

                                   
        public ServerBase(ClientInfo newClientInfo)
        {
            this.clientInfo = newClientInfo;
            DealClientTransaction(newClientInfo.client);
        }
        
        //处理客户端事务
        public void DealClientTransaction(TcpClient client)
        {
            this.client = client;
            Console.WriteLine("Connecting from client {0} ...", client.Client.RemoteEndPoint);

            //创建并启动一个新线程
            clientThread = new Thread(new ThreadStart(DealClient));
            clientInfo.clientThread = clientThread;
            clientThread.IsBackground = true;
            clientThread.Start();
        }
        //处理客户端任务具体过程
        public void DealClient() 
        {
            //try
            //{
                Message in_message;
                do
                {
                    //获取客户端输入流
                    dataStream = this.client.GetStream();
                    //解析输入数据
                    in_message = Message.Parse(dataStream);

                    switch (in_message.Command) 
                    { 
                        case Message.CommandHeader.LoginAuth:
                            ClientBusinessManager.Authenticate(dataStream,in_message,clientInfo);
                            break;
                        case Message.CommandHeader.AddUser:
                            UserBussinessManager.AddUser(dataStream, in_message, clientInfo);
                            break;
                        case Message.CommandHeader.SearchUser:
                            UserBussinessManager.SearchUser(dataStream, in_message, clientInfo);
                            break;
                        case Message.CommandHeader.AddPermission:
                            PermissionBussinessManager.AddPermission(dataStream, in_message, clientInfo);
                            break;
                        case Message.CommandHeader.SearchPermission:
                            PermissionBussinessManager.SearchPermission(dataStream, in_message, clientInfo);
                            break;
                            
                        case Message.CommandHeader.SearchSolution:
                            //ClientBusinessManager.SearchSolution(dataStream, in_message, clientInfo);
                            break;

                        case Message.CommandHeader.AddProject:
                            ProjectBussinessManager.AddProject(dataStream, in_message, clientInfo);
                            break;
                        case Message.CommandHeader.SearchProject:
                            ProjectBussinessManager.SearchProject(dataStream,in_message,clientInfo);
                            break;
                        case Message.CommandHeader.DeleteProject:
                            ProjectBussinessManager.deleteProject(dataStream,in_message,clientInfo);
                            break;
                        case Message.CommandHeader.PushXML:
                            PushBussinessManager.PushXML(dataStream, in_message, clientInfo);
                            break;
                        case Message.CommandHeader.PushXMLAck:
                            PushBussinessManager.PushXMLAck(dataStream, in_message, clientInfo);
                            break;
                        case Message.CommandHeader.Chat://测试
                            Chating(in_message);
                            break;
                        case Message.CommandHeader.GetXmlRequest:
                            //request from client ,server send xml to client
                            if (DataTransferBussinessManager.SendAckToClientGetXmlRequest(dataStream, in_message))
                            {
                                Console.WriteLine(Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0]+" "+
                                    Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1]);
                                if (DataTransferBussinessManager.SendXml(dataStream,
                                    Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0],
                                    Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1]))
                                {
                                    Console.WriteLine("xml send run here");
                                    Console.WriteLine("Send xml " + Encoding.Unicode.GetString(in_message.MessageBody));
                                }
                            }
                            break;
                        case Message.CommandHeader.SendXmlRequest:
                            //request from client , client send xml to server
                            if (DataTransferBussinessManager.SendAckToClientSendXmlRequest(dataStream, in_message))
                            {
                                if (DataTransferBussinessManager.GetXml(dataStream))
                                {
                                    Console.WriteLine("接受文件完成");

                                    //推送更新消息
                                    Reminder reminder = new Reminder();
                                    String solutionName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0];
                                    String projectName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1];
                                    String ReminderMessage = reminder.GenerateReminderMessage(solutionName,projectName);
                                    reminder.ReminderClient(projectName, ReminderMessage);


                                }
                            }
                            break;
                        case Message.CommandHeader.GetDocumentRequest:
                            if (DataTransferBussinessManager.SendAckToClientGetDocumentRequest(dataStream, in_message))
                            {
                                if (DataTransferBussinessManager.SendDocument(dataStream,
                                    Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0],
                                    Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1],
                                    Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[2]))
                                {
                                    Console.WriteLine("Send Document " + Encoding.Unicode.GetString(in_message.MessageBody));
                                }
                            }
                            break;
                        case Message.CommandHeader.SendDocumentRequest:
                            //request from client 
                            if (DataTransferBussinessManager.SendAckToClientSendDocumentRequest(dataStream, in_message))
                            {
                                if (DataTransferBussinessManager.GetDocument(dataStream))
                                {
                                    Console.WriteLine("成功接受文档");

                                    //推送更新消息
                                    Reminder reminder = new Reminder();
                                    String solutionName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[0];
                                    String projectName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[1];
                                    String documentName = Encoding.Unicode.GetString(in_message.MessageBody).Split(':')[2];
                                    reminder.ReminderClient(projectName, solutionName+":"+projectName+":"+documentName);

                                }
                            }
                            break;
                        case Message.CommandHeader.Hello:
                            //找到相应的客户端，修改客户端存活时间 
                            ClientThreadManager.SetClientAliveTime(this.client.Client.RemoteEndPoint.ToString());
                            ClientThreadManager.printState();//测试
                            break;
                        case Message.CommandHeader.Offline:
                            //找到相应的客户端，销毁线程，再从clientList中删除
                            if (ClientThreadManager.RemoveClientConnect(this.client.Client.RemoteEndPoint.ToString()))
                            {
                                Console.WriteLine(this.client.Client.RemoteEndPoint.ToString());
                                Console.WriteLine(this.client.Client.RemoteEndPoint + " is offline. - ServerBase.");
                                Console.WriteLine("终止连接成功");
                            }
                            
                            break;
                        case Message.CommandHeader.Sync:
                            break;
                        default:
                            //message wrong
                            break;
                    }
                }while(true);
            //}
            //catch (Exception ex) {
            //    Console.WriteLine("DealClient: " + ex.Message);
            //}
        }

        public void Chating(Message in_message)
        {
            Message out_message = new Message();
            Console.Write("Client @");
            Console.Write(client.Client.RemoteEndPoint);
            Console.WriteLine(" say:");
            Console.WriteLine(Encoding.Unicode.GetString(in_message.MessageBody));

            out_message = new Message();
            out_message.Command = Message.CommandHeader.Chat;
            Console.WriteLine("Server to Client:");
            out_message.MessageBody = Encoding.Unicode.GetBytes(Console.ReadLine());
            //打包输出信息,并将输出信息写入输出流
            dataStream.Write(out_message.ToBytes(), 0, out_message.MessageLength);
        }
       
    }
}
