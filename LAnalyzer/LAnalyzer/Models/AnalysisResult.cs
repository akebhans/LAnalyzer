using System.Collections.Generic;

namespace LAnalyzer.Models
{
    public class AnalysisResult
    {
        public string ProjectName { get; set; }
        public List<PropName> PropNameList { get; set; }
        public List<List<PropValue>> PropValueLists { get; set; }
        public List<PropRow> PropRowList { get; set; }
        public List<DataName> DataNameList { get; set; }
        public List<DataRow> DataRowList { get; set; }
        public string SqlString { get; set; }
        public List<string> PropertyList { get; set; }
        public List<double> DataList { get; set; }
        public List<ResultRow> ResultMatrix { get; set; }
    }

    public class ResultRow
    {
        public List<string> PropertyName { get; set; }
        public List<double> DataSum { get; set; }
    }

}

