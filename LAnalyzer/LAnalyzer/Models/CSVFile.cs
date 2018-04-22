using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LAnalyzer.Models
{
    public class CSVFile
    {
        public string Project { get; set; }
        public List<string> NameList { get; set; }
        public List<object> ValueList { get; set; }
        public List<string> TypeList { get; set; }

        public CSVFile() { }
    }
}