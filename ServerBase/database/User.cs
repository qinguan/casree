using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerBase.database
{
    class User
    {
        public string name;
        public string passwd;
        public int groupId;

        public User(string name, string passwd, int groupId)
        {
            this.name = name;
            this.passwd = passwd;
            this.groupId = groupId;
        }
    }
    /*
     * Projectinfo类的定义要移出User.cs，新建一个Projectinfo.cs文件
     */
    class Projectinfo
    {
        public string projectID;
        public string programID;
        public string projectDescription;
        public string projectType;

        public Projectinfo(string projectID, string programID, string projectDescription, string projectType)
        {
            this.projectID = projectID;
            this.programID = programID;
            this.projectDescription = projectDescription;
            this.projectType = projectType;
        }
    }
    /*
     *  Permission类的定义要移出User.cs，新建一个Permission.cs文件
     */
    /*
    class Permission
    {
        public string permissionID;
        public string username;
        public string projectID;
        public int  permissionlevel;
        public Permission()
        { }
        public Permission(string permissionID, string username, string projectID, int permissionlevel)
        {
            this.permissionID = permissionID;
            this.username = username;
            this.projectID = projectID;
            this.permissionlevel = permissionlevel;
        }
    }*/
     
}