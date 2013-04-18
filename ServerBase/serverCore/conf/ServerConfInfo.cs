using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;

namespace ServerBase
{
    class ServerConfInfo
    {
        private String confPath = Application.StartupPath + "\\server.conf";
        private String rootDirectory = Application.StartupPath + "\\CASREE";
        
        public String ConfPath
        {
            get { return confPath; }
            set { confPath = value; }
        }

        public String RootDirectory
        {
            get { return rootDirectory; }
            set { rootDirectory = value; }
        }
    }
}
