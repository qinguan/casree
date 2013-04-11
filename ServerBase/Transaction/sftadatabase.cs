using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerBase.Transaction
{
    class sftadatabase
    {
        public static SFTA_DATABASEDataContext SFTA_Database_Ctx = new SFTA_DATABASEDataContext();
        public sftadatabase()
        {
        }

        public static Boolean insertNodeInfo(string nodeid,string nodedataid,string nodename,string description,string NodeType,string parentID,string siblingID,string FMEAInfo)
        {
            var nodes = from sftainfo in SFTA_Database_Ctx.SFTATreeInfo
                        where sftainfo.nodeID == nodeid
                        select sftainfo;
            if (nodes.Count() != 0)
            {
                return false;//节点已经存在
            }
            else
            {
                SFTA_Database_Ctx.SFTATreeInfo.InsertOnSubmit(new SFTATreeInfo { nodeID = nodeid, nodedataID = nodedataid, nodename = nodename, description = description, NodeType = NodeType, parentID = parentID, siblingID = siblingID, FMEAInfo = FMEAInfo });
                //SFTA_Database_Ctx.SFTATreeInfo.InsertOnSubmit(new SFTATreeInfo(nodeid, nodedataid, nodename, description, NodeType, parentID, siblingID, FMEAInfo));
                SFTA_Database_Ctx.SubmitChanges();
            }
            return true;
        }
        public static Boolean insertNodeRelation(string nodeid, string childid)
        {
            SFTA_Database_Ctx.SFTANodeRelation.InsertOnSubmit(new SFTANodeRelation { nodeID = nodeid, childID = childid });
            return true;
        }
        //public static SFTATreeInfo queryNodeInfo(string nodeid)
        //{
        //    var nodes = from sftainfo in SFTA_Database_Ctx.SFTATreeInfo
        //                where sftainfo.nodeID == nodeid
        //                select sftainfo;
        //    if (nodes.Count() == 0)
        //    {
        //        return null;
        //    }

        //    return new SFTATreeInfo();
        //}
    }
}
