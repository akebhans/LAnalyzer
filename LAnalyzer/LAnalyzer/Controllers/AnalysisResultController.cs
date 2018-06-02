using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using LAnalyzer.Context;
using LAnalyzer.Models;
using System.Configuration;
using System.Data.SqlClient;


namespace LAnalyzer.Controllers
{
    // Controller for showing query results (when hitting 'CALCULATE' fo instance) 
    public class AnalysisResultController : Controller
    {
        private DB_Context db = new DB_Context();
        // GET: AnalysisResult, id is the number of the ProjectId
        public ActionResult Index(int id)
        {
            var calcFlag = Request.Form["calcFlag"];

            int noSel = 0;

            // List to hold PropertyId of the conditions selected
            List<int> condIdList = new List<int>();

            // List to hold the property value for each of the conditions selected
            List<List<PropValue>> condValueLists = new List<List<PropValue>>();

            // read number of conditions set
            if (Request.Form["noSelections"] != null)
            {
                noSel = int.Parse(Request.Form["noSelections"]);
            }

            // Fill list of PropertyId set in the conditions
            for (int i = 0; i < noSel; i++)
            {
                condIdList.Add(int.Parse(Request.Form["select_" + i.ToString()]));
            }

            // Get the propertynames from the project with ProjectId id
            var propertyNames = from pN in db.PropertyName
                                where pN.ProjectId.Equals(id)
                                select pN;

            //Initiate a new object of the AnalysisResult class(to hold values to be shown on the screen for th user)
            AnalysisResult myResult = new AnalysisResult();

            List<PropName> myPropNameList = new List<PropName>();

            // Fetch the values ofthe conditions set
            if (noSel > 0)
            {
                myResult.PropValueLists = GetCondValues(condIdList);
            }

            // Set the name of the project to the AnalysisResult object
            myResult.ProjectName = db.Project.Find(id).ProjectName;

            foreach (var item in propertyNames)
            {
                myPropNameList.Add(item);
            }

            // Set the Properties that to the AnalysisResult object
            myResult.PropNameList = myPropNameList;
            
            // Get the DataNames related to this project
            var dataNames = from dN in db.DataName
                            where dN.ProjectId.Equals(id)
                            select dN;

            List<DataName> myDataNameList = new List<DataName>();

            // Fill the DataNames in a list
            foreach (var item in dataNames)
            {
                myDataNameList.Add(item);
            }

            // Set the DataNames to the AnalysisResult object
            myResult.DataNameList = myDataNameList;


            // If 'CALCULATE' button has been hit...
            if (calcFlag == "YES")
            {

                List<string> groupByStr = new List<string>();
                List<string> sumByStr = new List<string>();

                // Get SQL-string corresponding to the conditions set by user
                string condSqlStr = GetSqlCondition();
                
                // Get SQL-strings based on input regarding Group By and Sum By respctively
                groupByStr = GetGroupByStr(condSqlStr);
                sumByStr = GetSumByStr(condSqlStr, id);

                // Create the resulttable (matrix) 
                myResult.ResultMatrix = GetMatrix(groupByStr, sumByStr);

            }

            return View(myResult);

        }

        // Creates the final SQL-code to be run to show the reults from hitting the CALCULATE button
        public List<ResultRow> GetMatrix(List<string> groupStrList, List<string> sumStrList)
        {
            //Create a list of resultrows
            List<ResultRow> retMatrix = new List<ResultRow>();

            // tempRow is a working object of resultRow where temporary results rows will be placed
            ResultRow tempRow = new ResultRow();
            string sqlStr = "", groupByString = "";

            // Loop through all Group By properties set by user
            bool isFirst = true;
            for (int i = 1; i < groupStrList.Count; i++)
            {
                // First Group By
                if (isFirst)
                {
                    sqlStr = "SELECT " + groupStrList[i];
                    groupByString = groupStrList[i];
                    isFirst = false;
                }
                // the following ones - just add Propert Name to SqL string
                else
                {
                    sqlStr += ", " + groupStrList[i]; 
                    groupByString += ", " + groupStrList[i];
                }
            }

            // Loop through all Sum By DataNames
            for (int i = 1; i < sumStrList.Count; i++)
            {
                sqlStr += ", SUM(" + sumStrList[i] + ")";
            }

            // Create the final SQL-string. NOTE groupStrList[0] contains the SQL-code after FROM defined by the GROUP BY input,
            // sumStrList[0] contains the corresponding SQL-code defined by the Sum By input.
            sqlStr += " FROM " + groupStrList[0] + " JOIN (" + sumStrList[0] + " ) SV1 ON V0.RowId = SV1.RowId GROUP BY " + groupByString;

            return GetResult(sqlStr);
        }

        public List<string> GetSumByStr(string myCondStr, int myProjId)
        // Gets the SQL-code (after FROM) defined by the SUM BY input, it also gives the SUM BY properties in a list
        // myCondStr is the SQL code giving the RowNo's corresponding to the conditions set by the user
        {
            List<string> retStr = new List<string>(); 
            //List<string> groupByList = new List<string>();
            List<string> sumByList = new List<string>();

            string sumByStr = "", sqlStart = "", sqlWhere = "";

            int noSumBy = 0, i;

            // Get number of SUM BY properties set
            if (Request.Form["noSumBy"] != null)
            {
                noSumBy = int.Parse(Request.Form["noSumBy"]);
            }

            // Create SQL-code for each SUM BY property defined. Also save the in a list to be able to later create the SUM(X1), SUM(X2).... SQL code
            bool tempFirst = true;
            for (i = 0; i <= noSumBy; i++)
            {
                if (Request.Form["sumBy_" + i.ToString()] != null)
                {
                    // Add Sum By property to list
                    sumByList.Add("DV_" + i.ToString());

                    // Create JOIN SQL code for each SUM BY property set
                    if (tempFirst)
                    {
                        sumByStr = myCondStr + " CondView LEFT JOIN DataRows DR_" + i.ToString() + " ON CondView.RowId = DR_" + i.ToString() + ".RowId" +
                            " JOIN DataNames DN_" + i.ToString() + " ON DR_" + i.ToString() + ".DataId = DN_" + i.ToString() + ".DataId";
                        sqlStart = "SELECT CondView.RowId, DR_" + i.ToString() + ".DataValue AS DV_" + i.ToString();
                        sqlWhere = " WHERE DN_" + i.ToString() + ".ProjectId = " + myProjId.ToString() + " AND DN_" + i.ToString() + ".DataId = " + Request.Form["sumBy_" + i.ToString()];
                        tempFirst = false;
                    }
                    else
                    {
                        sumByStr += " LEFT JOIN DataRows DR_" + i.ToString() + " ON CondView.RowId = DR_" + i.ToString() + ".RowId" +
                                    " JOIN DataNames DN_" + i.ToString() + " ON DR_" + i.ToString() + ".DataId = DN_" + i.ToString() + ".DataId";
                        sqlStart += ", DR_" + i.ToString() + ".DataValue AS DV_" + i.ToString();
                        sqlWhere += " AND DN_" + i.ToString() + ".ProjectId = " + myProjId.ToString() + " AND DN_" + i.ToString() + ".DataId = " + Request.Form["sumBy_" + i.ToString()];
                    }
                }

            }
            sqlStart += " FROM ";

             
            retStr.Add(sqlStart + sumByStr + sqlWhere);


            i = 0;
            foreach (var term in sumByList)
            {
                retStr.Add(sumByList[i]);
                i++;
            }

            // Return the part of the final SQL-code starting with "FROM ... " upto but not including 'GROUP BY' in index 0 in the list
            // In the following indexes return the SUM BY Properties
            return retStr;
        }

        public List<string> GetGroupByStr(string myCondStr)
        // Returns the SQLcode defined by the GROUP BY selections done by the user
        //
        {
            List<string> retStr = new List<string>();
            List<string> groupByList = new List<string>();

            int noGroupBy = 0;

            // Get number of Group By set
            if (Request.Form["noGroupBy"] != null)
            {
                noGroupBy = int.Parse(Request.Form["noGroupBy"]);
            }

            string preGroupBy = "", subGroupBy = "", oneGroupBy = "";

            // oneGroupBy is a SQL-code template defining the basic code to be modified for each Group By property set.
            // [_GroupBy_] and #_PropertyId# are just placeholders to be replaced when looping through each group By property.
            oneGroupBy = "SELECT CV.ROWID, PV.PropertyValue AS [_GroupBy_], PN.ProjectId FROM " + myCondStr + " CV LEFT JOIN " +
                "PropRows PR ON CV.RowID = PR.RowID LEFT JOIN " +
                "(SELECT* FROM PropValues WHERE PropertyId = #_PropertyId#) PV ON PR.PropertyValueId = PV.PropertyValueId JOIN PropNames PN ON PV.PropertyId = PN.PropertyId";

            bool firstGroupBy = true;
            for (int i = 0; i <= noGroupBy; i++)
            {
                if (Request.Form["groupBy_" + i.ToString()] != null)
                {
                    string tempProperty = Request.Form["groupBy_" + i.ToString()].ToString();
                    if (tempProperty != "0")
                    {
                        string tempSql = oneGroupBy;
                        // replace the placeholders
                        tempSql = tempSql.Replace("#_PropertyId#", tempProperty);
                        tempSql = tempSql.Replace("_GroupBy_", "Prop_" + i.ToString());
                        // proGroupBy is defined by GROUP BY selection and holds JOINS to be able to get the group values 
                        // subGroupBy holds a list of virtual names of the properties selected in GROUP BY
                        if (firstGroupBy)
                        {
                            preGroupBy = "(" + tempSql + ") V" + i.ToString();
                            subGroupBy = "Prop_" + i.ToString();
                            firstGroupBy = false;
                        }
                        else
                        {
                            preGroupBy = preGroupBy + " JOIN (" + tempSql + ") V" + i.ToString() + " ON V" + (i - 1).ToString() + ".ProjectId = V" + i.ToString() + ".ProjectId AND " +
                                "V" + (i - 1).ToString() + ".RowId = V" + i.ToString() + ".RowId"
                                ;
                            subGroupBy = subGroupBy + ", " + "Prop_" + i.ToString();
                        }
                    }
                }
            }

            retStr.Add(preGroupBy);
            retStr.Add(subGroupBy);
            
            return retStr;
        }

        public List<ResultRow> GetResult(string sqlStr)
        // Evaluates the SQl string sqlStr and returns a list of resultRows
        {
            string ADOcon = ConfigurationManager.ConnectionStrings["ADOLucroAnalyzer"].ConnectionString;
            SqlConnection DbConnection = new SqlConnection(ADOcon);
            DbConnection.Open();

            SqlCommand resultCmd = new SqlCommand(sqlStr, DbConnection);
            SqlDataReader myReader;

            try
            {
                myReader = resultCmd.ExecuteReader();
            }
            catch (System.Data.SqlClient.SqlException e)
            {
                return null;
            }

            List<ResultRow> myResult = new List<ResultRow>();


            while (myReader.Read())
            {
                List<string> myPropRow = new List<string>();
                List<double> myDataRow = new List<double>();
                ResultRow myResultRow = new ResultRow();

                for (int i = 0; i < myReader.FieldCount; i++)
                {
                    if (myReader[i] is string)
                    {
                        myPropRow.Add(myReader[i].ToString());
                    }
                    else
                    {
                        myDataRow.Add(double.Parse(myReader[i].ToString()));
                    }
                }

                myResultRow.PropertyName = myPropRow;
                myResultRow.DataSum = myDataRow;
                myResult.Add(myResultRow);
            }

            return myResult;
        }

        private string GetPropName(int propId)
        {
            PropName myProperty = new PropName();
            myProperty.PropertyName = db.PropertyName.Find(propId).PropertyName;

            return myProperty.PropertyName;
        }

        public string GetSqlCondition()
        // Based on the Conditions set by the user, this method returns the code the gives the RowNo's corresponding to the conditions defined.
        {
            string sqlStr = "", cWhere = "", locSelection, tempWhere;
            int noSel = 0;
            if (Request.Form["noSelections"] != null)
            {
                noSel = int.Parse(Request.Form["noSelections"]);
            }

            for (int i = 0; i < noSel; i++)
            {
                int j = 0;
                tempWhere = "";

                if (sqlStr == "")
                {
                    sqlStr = "SELECT T0.RowID FROM PropRows T" + i.ToString() + " ";
                }
                else
                {
                    sqlStr = sqlStr + " JOIN PropRows T" + i.ToString() + " ON T" + (i - 1).ToString() + ".RowId = T" + i.ToString() + ".RowId";
                }

                while (Request.Form["selValue_" + i.ToString() + "_" + j.ToString()] != null)
                {
                    locSelection = Request.Form["selValue_" + i.ToString() + "_" + j.ToString()];
                    if (locSelection != "0")
                    {
                        if (tempWhere == "")
                        {
                            if (cWhere != "") tempWhere = " AND ";
                            tempWhere = tempWhere + "T" + i.ToString() + ".PropertyValueId IN (" + locSelection;
                        }
                        else if (locSelection != "0") tempWhere = tempWhere + ", " + locSelection;
                        if (cWhere == "") cWhere = " WHERE ";
                    }
                    j++;
                }
                if (tempWhere != "")
                {
                    tempWhere = tempWhere + ") ";
                    cWhere = cWhere + tempWhere;
                }
            }
            if (sqlStr != "")
            {
                sqlStr = "(" + sqlStr + cWhere + ") ";
            }
            else
            {
                sqlStr = "(SELECT DISTINCT RowID FROM PropRows) ";
            }
            return sqlStr;
        }

        public List<List<PropValue>> GetCondValues(List<int> cIdList)
        // Based on which properties (cIdList) selected, this method returns a list of valid Property values for each Property Id in the cIdList
        {
            List<List<PropValue>> retPropValues = new List<List<PropValue>>();
            foreach (var item in cIdList)
            {
                List<PropValue> locList = new List<PropValue>();
                var propValues = from pV in db.PropertyValue
                                 where pV.PropertyId.Equals(item)
                                 select pV;
                foreach (var term in propValues)
                {
                    locList.Add(term);
                }
                retPropValues.Add(locList);
            }
            return retPropValues;
        }
    }
}
