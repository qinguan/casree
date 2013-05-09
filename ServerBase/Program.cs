﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Threading;

using ServerBase.database;
using System.Windows.Forms;

namespace ServerBase
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerManager sm = new ServerManager();
            sm.ServerStart(new IPAddress(new byte[] { 127, 0, 0, 1 }), 8500);
            //Console.WriteLine(Database.queryProgramIdByProjectId("1"));
            //Console.WriteLine(Application.StartupPath + "\\CASREE");
            //sm.ServerStart(new IPAddress(new byte[] { 192, 168, 241, 48 }), 8860);



            //database.Database.insertUser("jun", "123", 1);
            //database.Database.insertUser("guo", "123", 1);
            //database.User u = database.Database.queryUser("xu");
            //Console.WriteLine(u.name);
            //Console.WriteLine(u.passwd);
            //Console.WriteLine(u.groupId);

            //database.Database.insertSolutionProject("test1", "Assessment"); ;
            //List<string> t = database.Database.querySolutionProject("test1");
            //foreach (var s in t) {
            //    Console.WriteLine(s);
            //}
            



        }
    }
}
