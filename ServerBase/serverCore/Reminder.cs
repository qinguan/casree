using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ServerBase.database;
using System.Windows.Forms;

namespace ServerBase
{
    public class Reminder
    {
        PushRuleInfo pri = new PushRuleInfo();
        String configuretionFile = Application.StartupPath + "\\PushRules.xml";

        public void PollTask()
        {
            foreach (KeyValuePair<string, ClientInfo> s in ClientThreadManager.clientList)
            {
                String programId = Database.queryProgramIdByProjectId(s.Value.projectid);
                List<string> projectList = Database.querySolutionProject(programId);
            }
        }

        //初始化推送规则
        public void init()
        {
            PushRule.ParseRule(pri,configuretionFile);
        }
    }
}
