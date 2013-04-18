using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace ServerBase
{
    public class PushRuleInfo
    {
        public enum Project : byte
        {
            Predict,
            Assign,
            Analysis,
            Design,
            Test,
            Assess
        }

        private Dictionary<string, ArrayList> pushRules = new Dictionary<string, ArrayList>();
        private String configurationFile = String.Empty;


        public Dictionary<string, ArrayList> PushRules
        {
            get { return pushRules; }
            set { pushRules = value; }
        }

        public String ConfigurationFile
        {
            get { return configurationFile; }
            set { configurationFile = value; }
        }
    }
}
