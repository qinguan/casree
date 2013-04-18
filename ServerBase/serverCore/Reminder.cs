using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using ServerBase.database;
using System.Windows.Forms;

namespace ServerBase
{
    public class Reminder
    {
        PushRuleInfo pri = new PushRuleInfo();
        String confFile = Application.StartupPath + "\\PushRules.xml";

        public String ConfFile
        {
            get { return confFile; }
            set { confFile = value; }
        }

        public Reminder()
        {
            init();
        }

        //初始化推送规则
        public void init()
        {
            PushRule.ParseRule(pri, ConfFile);
        }

        public void ReminderClient(String projectId, String reminderMessage)
        {
            //先查rule，找出推送destination,即target project client
            ArrayList targetList = new ArrayList();
            foreach (String key in pri.PushRules.Keys)
            {
                if (key.Equals(projectId))
                {
                    targetList = pri.PushRules[key];
                }
            }

            //再查clientList，找出target client
            foreach (KeyValuePair<string, ClientInfo> c in ClientThreadManager.clientList)
            {
                if (targetList.Contains(c.Value.projectid))
                {
                    //向target client发送更新消息
                    PushBussinessManager.ReminderPush(c.Value, reminderMessage);
                }
            }
        }

        public String GenerateReminderMessage(String solutionName, String projectName)
        {
            return solutionName + ":" + projectName;
        }

       
    }
}
