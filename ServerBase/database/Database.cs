using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerBase.database
{
    class Database
    {
        public static CASREE_DatabaseDataContext CASREE_DatabaseCtx = new CASREE_DatabaseDataContext();

        public Database() 
        {

        }

        #region casree_user table

        public static Boolean insertUser(string userName, string userPasswd, Int32 userGroupId) 
        {
            if (queryUser(userName) == null)
            {
                CASREE_DatabaseCtx.users.InsertOnSubmit(
                    new user
                    {
                        UserName = userName,
                        Password = userPasswd,
                        GroupID = userGroupId
                    });
                CASREE_DatabaseCtx.SubmitChanges();
                return true;
            }
            else {
                Console.WriteLine(userName + "exist in the database.");
                return false;
            }
            
        }

        public static User queryUser(string userName)
        {
            var users = from user in CASREE_DatabaseCtx.users
                    where user.UserName == userName
                    select user;

            if (users.Count() == 0)
            {
                return null;
            }

            return new User(users.First().UserName.Split(' ')[0], users.First().Password.Split(' ')[0], users.First().GroupID);                      
        }

        public static Boolean updateUser(string userName, string userPasswd, Int32 userGroupId) 
        {
            IQueryable<user> userToUpdate = from user in CASREE_DatabaseCtx.users
                                                   where user.UserName == userName
                                                   select user;
            if (userToUpdate.Count() != 0) {
                userToUpdate.First().UserName = userName;
                userToUpdate.First().Password = userPasswd;
                userToUpdate.First().GroupID = userGroupId;
                CASREE_DatabaseCtx.SubmitChanges();
                return true;
            }
            return false;
        }

        public static Boolean deleteUser(string userName) 
        {
            IQueryable<user> usersToDelete = from user in CASREE_DatabaseCtx.users
                                                    where user.UserName == userName
                                                    select user;
            if (usersToDelete.Count() > 0)
            {
                CASREE_DatabaseCtx.users.DeleteOnSubmit(usersToDelete.First());
                CASREE_DatabaseCtx.SubmitChanges();
                return true;
            }
            return false;
        }

        #endregion

        #region casree_permission table

        public static Boolean insertPermission(string userName,string userProjectName, int level)
        {
            //if (queryPermission(userName) == null)
            //{
                CASREE_DatabaseCtx.permissions.InsertOnSubmit(
                    new permission
                    {
                        UserName = userName,
                        ProjectID = userProjectName,
                        PermissionID=Guid.NewGuid().ToString(),
                        PermissionLevel=level
                    });
                CASREE_DatabaseCtx.SubmitChanges();
                return true;
            //}
            //else 
            //{
            //    Console.WriteLine(userName + " exist.");
            //    return false;
            //}
            
        }

        public static List<Permission> queryPermission(string userName)
        {//检索权限
            var permissions = from permission in CASREE_DatabaseCtx.permissions
                              where permission.UserName == userName
                              select permission;
            Console.WriteLine("username : " + userName);
            if (permissions.Count() == 0)
            {
                return null;
            }

            List<Permission> permissionList= new List<Permission>();

           
            foreach (var pm in permissions)
            {
                Console.WriteLine(pm.UserName + " " + pm.PermissionID + " " + pm.PermissionLevel);
            }
        
            
            foreach (var pm in permissions) {
                permissionList.Add(
                    new Permission(
                        pm.UserName.Split(' ')[0],
                        pm.ProjectID.Split(' ')[0],
                        pm.PermissionLevel)
                    );
            }
            return permissionList;

        }

        /// <summary>
        /// 根据工具类型和用户名查找权限
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="prjType">工具类型</param>
        /// <returns></returns>
        public static List<Permission> queryPermissionbyPrjtype(string userName,string prjType)
        {//检索权限
            var permissions = from permission in CASREE_DatabaseCtx.permissions
                              where permission.UserName == userName
                              select permission;//检索用户有权限访问的工程

            var projects = from solution in CASREE_DatabaseCtx.ProjectInfos
                           where solution.ProjectType == prjType
                           select solution;//检索该工具的现有工程列表

            Console.WriteLine("username : " + userName);
            if (permissions.Count() == 0)
            {//用户可访问的工程列表为空
                return null;
            }
            if (projects.Count() == 0)//工具下的工程列表为空
                return null;


            List<Permission> permissionList = new List<Permission>();//权限列表


            foreach (var pm in permissions)
            {
                Console.WriteLine(pm.UserName + " " + pm.PermissionID + " " + pm.PermissionLevel);
            }


            foreach (var pm in permissions)
            {//对于每一个用户有权限访问的工程
                foreach(var pj in projects)
                    if (pj.ProjectID == pm.ProjectID)
                    {//看对应的工具下是否有该工程
                        permissionList.Add(
                            new Permission(
                                pm.UserName.Split(' ')[0],
                                pm.ProjectID.Split(' ')[0],
                                pm.PermissionLevel));
                        break;
                    }
            }
            return permissionList;

        }

        public static List<Permission> queryPermissionbyPrjID(string userName,string projectID)
        {//检索权限
            var permissions = from permission in CASREE_DatabaseCtx.permissions
                              where permission.UserName == userName&&permission.ProjectID==projectID
                              select permission;
            Console.WriteLine("username : " + userName+" projectID:"+projectID);
            if (permissions.Count() == 0)
            {
                return null;
            }

            List<Permission> permissionList = new List<Permission>();

            foreach (var pm in permissions)
            {
                permissionList.Add(
                    new Permission(
                        pm.UserName.Split(' ')[0],
                        pm.ProjectID.Split(' ')[0],
                        pm.PermissionLevel)
                    );
            }
            return permissionList;

        }

        public static Boolean updatePermission(string userName, string projectName,int permissionLevel)
        {
            IQueryable<permission> permissionToUpdate = 
                from permission in CASREE_DatabaseCtx.permissions
                where permission.UserName == userName
                select permission;

            if (permissionToUpdate.Count() > 0) {
                permissionToUpdate.First().UserName = userName;
                permissionToUpdate.First().ProjectID = projectName;
                permissionToUpdate.First().PermissionLevel = permissionLevel;
                CASREE_DatabaseCtx.SubmitChanges();
                return true;
            }
            return false;
            
        }

        public static Boolean deletePermission(string userName)
        {
            IQueryable<permission> permissionToDelete =
                from permission in CASREE_DatabaseCtx.permissions
                where permission.UserName == userName
                select permission;

            if (permissionToDelete.Count() > 0)
            {
                CASREE_DatabaseCtx.permissions.DeleteOnSubmit(permissionToDelete.First());
                CASREE_DatabaseCtx.SubmitChanges();
                return true;
            }
            return false;
        }

        #endregion

        #region casree_projectinfo table

        public static Boolean deleteProject(string username, string projectid, string programid)
        {

           /* IQueryable<ProjectInfo> projectToDelete =
                from project in CASREE_DatabaseCtx.ProjectInfo
                where project.ProjectID == projectid && project.ProgramID == programid
                select project;

            if (projectToDelete.Count() > 0)
            {
                CASREE_DatabaseCtx.ProjectInfo.DeleteOnSubmit(projectToDelete.First());
                CASREE_DatabaseCtx.SubmitChanges();
                return true;
            }
            return false;
        */
            IQueryable<permission> permissionToDelet =
                from permission in CASREE_DatabaseCtx.permissions
                where permission.ProjectID == projectid
                select permission;
            if (permissionToDelet.Count() > 0)
            {
                CASREE_DatabaseCtx.permissions.DeleteOnSubmit(permissionToDelet.First());
                CASREE_DatabaseCtx.SubmitChanges();
             }
         

            IQueryable<ProjectInfo> projectToDelete =
                from project in CASREE_DatabaseCtx.ProjectInfos
                where project.ProjectID == projectid && project.ProgramID == programid
                select project;

            if (projectToDelete.Count() > 0)
            {
                CASREE_DatabaseCtx.ProjectInfos.DeleteOnSubmit(projectToDelete.First());
                CASREE_DatabaseCtx.SubmitChanges();
                return true;
            }
            return false;
        
   
   
            
    
           

            
        }

        public static Boolean insertSolutionProject(string solutionname, string projectname,string description,string type)
        {
            if (querySolutionProject(solutionname) != null) 
            {
                foreach (var s in querySolutionProject(solutionname)) 
                {
                    if (s.Contains(projectname)) {
                        Console.WriteLine(projectname + " already exists in " + solutionname);
                        return false;
                    }
                }
            }
  
            CASREE_DatabaseCtx.ProjectInfos.InsertOnSubmit(
                new ProjectInfo
                {
                   
                    ProjectID = projectname,
                    ProgramID = solutionname,
                    ProjectDescription=description,
                    ProjectType=type
                });
            CASREE_DatabaseCtx.SubmitChanges();
            return true;
        }

        /// <summary>
        /// 返回programID下的projectID列表
        /// </summary>
        /// <param name="solutionName">programID</param>
        /// <returns>List<string> project</returns>
        public static List<string> querySolutionProject(string solutionName)
        {
            var solutions = from solution in CASREE_DatabaseCtx.ProjectInfos
                            where solution.ProgramID == solutionName
                            select solution;

            if (solutions.Count() == 0)
            {
                return null;
            }
            List<string> projects = new List<string>();
            foreach(var s in solutions){
                projects.Add(s.ProjectID);
            }
            
            return projects;
        }

        public static List<ProjectInfo> querySolution(string solutionName)
        {
            var solutions = from solution in CASREE_DatabaseCtx.ProjectInfos
                            where solution.ProgramID == solutionName
                            select solution;

            if (solutions.Count() == 0)
            {
                return null;
            }
            List<ProjectInfo> projectinfos = new List<ProjectInfo>();
            foreach(var s in solutions){
                projectinfos.Add(s);
            }
            
            return projectinfos;
        }

        public static Projectinfo queryProjectInfo(string projectID, string ProgramID)
        {
            var projectinfors = from projectinfo in CASREE_DatabaseCtx.ProjectInfos
                                where projectinfo.ProjectID == projectID && projectinfo.ProgramID == ProgramID
                                select projectinfo;
            if (projectinfors.Count() == 0)
            {
                return null;
            }
            return new Projectinfo(projectinfors.First().ProjectID.Split(' ')[0],projectinfors.First().ProgramID.Split(' ')[0],projectinfors.First().ProjectDescription.Split(' ')[0],projectinfors.First().ProjectType);
        }

        public static Projectinfo queryProjectByType(string solutionID, string type)
        {
            var projectinfors = from projectinfo in CASREE_DatabaseCtx.ProjectInfos
                                where projectinfo.ProjectType == type && projectinfo.ProgramID ==solutionID
                                select projectinfo;
            if (projectinfors.Count() == 0)
            {
                return null;
            }
            return new Projectinfo(projectinfors.First().ProjectID.Split(' ')[0], projectinfors.First().ProgramID.Split(' ')[0], projectinfors.First().ProjectDescription.Split(' ')[0], projectinfors.First().ProjectType);
        }

        public static string queryTypeByProjectID(string projectid)
        {
            var projectinfors = from projectinfo in CASREE_DatabaseCtx.ProjectInfos
                                where projectinfo.ProjectID == projectid
                                select projectinfo;
            if (projectinfors.Count() != 1)
                return null;
            return projectinfors.First().ProjectType;
        }
        #endregion
    }

    
}
