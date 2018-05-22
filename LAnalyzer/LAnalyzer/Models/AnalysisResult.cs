using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LAnalyzer.Models
{
    public class AnalysisResult
    {
        public string ProjectName {get; set;}
        public List<PropName> PropNameList { get; set; }
        public List<List<PropValue>> PropValueLists { get; set; }
        public List<PropRow> PropRowList { get; set; }
        public List<DataName> DataNameList { get; set; }
        public List<DataRow> DataRowList { get; set; }
    }
}