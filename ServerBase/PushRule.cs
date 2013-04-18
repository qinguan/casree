using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using System.Xml;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using System.IO;


namespace ServerBase
{
    public class PushRule
    {
        public static void ReloadRule(PushRuleInfo pri,String configurationFile)
        {
            pri.PushRules.Clear();
            ParseRule(pri,configurationFile);
        }
        
        public static void ParseRule(PushRuleInfo pri,String configurationFile)
        {
            XmlDocument doc = new XmlDocument();
            string xmlPath = configurationFile;
            doc.Load(xmlPath);
            
            XmlNodeList ruleNodes = doc.SelectNodes("rules/rule");
            int ruleNumber = ruleNodes.Count;

            for (int i = 0; i < ruleNumber; i++)
            {
                ArrayList dst = new ArrayList();

                XmlNodeList dstNodes = ruleNodes[i].SelectNodes("rules/rule/destination");
                for(int k=0;k<dstNodes.Count;k++)
                {
                    dst.Add(dstNodes[i].InnerText);
                }
                pri.PushRules.Add(ruleNodes[i].Attributes["source"].Value,dst);
            }
        }
    }
}
