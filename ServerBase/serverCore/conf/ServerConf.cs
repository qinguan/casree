using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using System.IO;


namespace ServerBase
{
    public class ServerConf
    {
        public static void ReloadRule(ServerConfInfo sci, String configurationFile)
        {
            sci = new ServerConfInfo();
            ParseServerConf(sci, configurationFile);
        }

        public static void ParseServerConf(ServerConfInfo sci,String configurationFile)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(configurationFile);

            XmlNode rootDirectory = doc.SelectSingleNode("rootDirectory");
            if (!rootDirectory.Equals(null))
            {
                sci.RootDirectory = rootDirectory.InnerText;
            }

            //后续服务器相关配置可添加于此
        }



    }
}
