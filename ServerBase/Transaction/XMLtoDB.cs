using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Xml;

namespace ServerBase.Transaction
{
    /// <summary>
    /// 处理各工具以ＸＭＬ格式传给服务器的数据并存储至数据库
    /// </summary>
    class XMLtoDB
    {
        public XMLtoDB(string path)
        {
            this.SFTA(path);
        }
        private Boolean SFTA(string path)
        {
            //XmlDocument doc = new XmlDocument();
            //// 装入指定的XML文档  
            //doc.Load(path);// (@"E:\CASREE\server\CASREE_V_2012Winter\ServerBase\bin\Debug\DefaultSolution\projectfta\projectfta__2012_11_26_12_09_52.xml");

            //XmlNodeList topnodes = doc.DocumentElement.ChildNodes;

            //foreach(XmlElement topnode in topnodes)//顶层节点
            //{
                    
            //        if(topnode.Name.Equals("SFTATreeNodes"))
            //        {
            //            foreach (XmlElement sftanodeinfo in topnode.ChildNodes)
            //            {
            //                string nodeid = string.Empty;
            //                string nodedataid = string.Empty;
            //                string nodename = string.Empty;
            //                string description = string.Empty;
            //                string nodetype = string.Empty;
            //                string parentid = string.Empty;
            //                string siblingid = string.Empty;
            //                string fmeainfo = string.Empty;
            //                List<string> childrenids = new List<string>();
            //                foreach (XmlElement info in sftanodeinfo.ChildNodes)
            //                {
            //                    if (info.Name.Equals("nodeID"))
            //                    {
            //                        nodeid = info.InnerText;
            //                    }
            //                    else if (info.Name.Equals("noderelation"))
            //                    {
            //                        foreach (XmlElement data in info.ChildNodes)
            //                        {
            //                            if (data.Name.Equals("parentNodeID"))
            //                                parentid = data.InnerText;
            //                            else if (data.Name.Equals("childrenNodesID"))
            //                                foreach (XmlElement childid in data.ChildNodes)
            //                                    childrenids.Add(childid.InnerText);
            //                            else if (data.Name.Equals("siblingID"))
            //                                siblingid = data.InnerText;
            //                        }
            //                    }
            //                    if (info.Name.Equals("nodedata"))
            //                    {
            //                        foreach (XmlElement data in info.ChildNodes)
            //                        {
            //                            if (data.Name.Equals("nodeataid"))
            //                                nodedataid = data.InnerText;
            //                            else if (data.Name.Equals("description"))
            //                                description = data.InnerText;
            //                            else if (data.Name.Equals("nodename"))
            //                                nodename = data.InnerText;
            //                            else if (data.Name.Equals("eventType"))
            //                                nodetype = data.InnerText;
            //                            else if (data.Name.Equals("FMEAInfo"))
            //                                fmeainfo = data.InnerText;
            //                        }
            //                    }
            //                }
            //                sftadatabase.insertNodeInfo(nodeid, nodedataid, nodename, description, nodetype, parentid, siblingid, fmeainfo);
            //                foreach (string child in childrenids)
            //                    sftadatabase.insertNodeRelation(nodeid, child);
            //            }
            //    }
            //    }
            
            return false;
        }
       
    }
}
