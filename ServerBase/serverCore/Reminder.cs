using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerBase
{
    class Reminder
    {
        public void PollTask()
        {
            foreach (KeyValuePair<string, ClientInfo> s in ClientThreadManager.clientList)
            {
                //s.Value.projectid
            }
        }

        //初始化推送规则
        public void init()
        {
 
        }
    }
}
