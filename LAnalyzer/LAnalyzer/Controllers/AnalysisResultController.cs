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
    public class AnalysisResultController : Controller
    {
        private DB_Context db = new DB_Context();
        // GET: AnalysisResult
        public ActionResult Index(int id)
        {
            var calcFlag = Request.Form["calcFlag"];

            int noSel = 0;

            List<int> condIdList = new List<int>();
            List<List<PropValue>> condValueLists = new List<List<PropValue>>();

            if (Request.Form["noSelections"] != null)
            {
                noSel = int.Parse(Request.Form["noSelections"]);
            }

            for (int i = 0; i < noSel; i++)
            {
                condIdList.Add(int.Parse(Request.Form["select_" + i.ToString()]));
            }


            var propertyNames = from pN in db.PropertyName
                                where pN.ProjectId.Equals(id)
                                select pN;

            AnalysisResult myResult = new AnalysisResult();
            List<PropName> myPropNameList = new List<PropName>();

            if (noSel > 0)
            {
                myResult.PropValueLists = GetCondValues(condIdList);
            }

            myResult.ProjectName = db.Project.Find(id).ProjectName;
            foreach (var item in propertyNames)
            {
                myPropNameList.Add(item);
            }

            myResult.PropNameList = myPropNameList;

            var dataNames = from dN in db.DataName
                            where dN.ProjectId.Equals(id)
                            select dN;

            List<DataName> myDataNameList = new List<DataName>();

            foreach (var item in dataNames)
            {
                myDataNameList.Add(item);
            }

            myResult.DataNameList = myDataNameList;



            if (calcFlag == "YES")
            {
                myResult.SqlString = GetSqlSumBy(GetSqlCondition(), id);
                //if (calcFlag == "YES") myResult.SqlString = GetSqlCondition();

                if (myResult.SqlString != "") myResult.ResultMatrix = GetResult(myResult.SqlString);

            }

            return View(myResult);

        }

        public List<ResultRow> GetResult(string sqlStr)
        {
            string ADOcon = ConfigurationManager.ConnectionStrings["ADOLucroAnalyzer"].ConnectionString;
            SqlConnection DbConnection = new SqlConnection(ADOcon);
            DbConnection.Open();

            SqlCommand resultCmd = new SqlCommand(sqlStr, DbConnection);

            SqlDataReader myReader = resultCmd.ExecuteReader();

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

            //foreach (var term in myResult)
            //{
            //    List<string> myPropRow = new List<string>();
            //    List<double> myDataRow = new List<double>();
            //    if (term is ResultRow)
            //    {

            //    }
            //    if (term is string) myPropRow.Add
            //}

            return myResult;
        }

        public string GetSqlSumBy(string condSql, int id)
        {
            string groupByStr = "", retStr = "", sqlStart = "", joinStr = "", tempProperty;
            List<string> sqlGroupBy = new List<string>();
            List<string> groupByList = new List<string>();
            List<string> sumByList = new List<string>();
            int noGroupBy = 0, noSumBy = 0, i;
            if (Request.Form["noGroupBy"] != null)
            {
                noGroupBy = int.Parse(Request.Form["noGroupBy"]);
            }

            if (Request.Form["noSumBy"] != null)
            {
                noSumBy = int.Parse(Request.Form["noSumBy"]);
            }



            groupByStr = "SELECT DR.DataValue, DR.DataId,DR.RowId ,PV.PropertyId,PV.PropertyValue, PN.ProjectId " +
                " FROM DataRows DR JOIN " + condSql + " CondView ON DR.RowId = CondView.RowId JOIN PropRows PR ON CondView.RowId = PR.RowId " +
                " JOIN PropValues PV ON PR.PropertyValueId = PV.PropertyValueId " +
                " JOIN PropNames PN ON PV.PropertyId = PN.PropertyId " +
                " JOIN DataNames DN ON PN.ProjectId = DN.ProjectId AND DR.DataId = DN.DataId " +
                " WHERE PN.ProjectId = " + id.ToString();

            // FORTSÄTT HÄR!!
            bool firstGroupBy = true;
            for (i = 0; i <= noGroupBy; i++)
            {
                if (Request.Form["groupBy_" + i.ToString()] != null)
                {
                    tempProperty = Request.Form["groupBy_" + i.ToString()].ToString();
                    if (tempProperty != "0")
                    {
                        sqlGroupBy.Add(groupByStr + " AND PV.PropertyId = " + tempProperty);
                        groupByList.Add(tempProperty);

                        if (firstGroupBy)
                        {
                            //sqlStart = GetPropName(int.Parse(tempProperty));
                            sqlStart = " T1.PropertyValue";
                            firstGroupBy = false;
                        }
                        else
                        {
                            //sqlStart = sqlStart + ", " + GetPropName(int.Parse(tempProperty));
                            sqlStart = sqlStart + ", T" + (i + 1).ToString() + ".Propertyvalue ";
                        }
                    }

                }
            }

            for (i = 0; i <= noSumBy; i++)
            {
                if (Request.Form["sumBy_" + i.ToString()] != null)
                {
                    sumByList.Add(Request.Form["sumBy_" + i.ToString()]);
                }

            }

            bool firstSqlGroupBy = true;
            i = 1;
            string cT;
            foreach (var term in sqlGroupBy)
            {
                if (firstSqlGroupBy)
                {
                    retStr = " FROM (" + term;
                    if (sqlGroupBy.Count == 1) retStr = retStr + ") T1 ";
                    firstSqlGroupBy = false;
                }
                else
                {
                    cT = "T" + i.ToString();
                    retStr = retStr + ") T1 JOIN (" + term + ") " + cT +
                        " ON T1.RowId = " + cT + ".RowId AND T1.ProjectId = " + cT + ".ProjectId AND T1.DataId = " + cT + ".DataId ";
                }
                i++;
            }

            //retStr = retStr + "GROUP BY " + sqlStart;

            // Just alllow one sum result
            bool tempFirst = true;
            foreach (var term in sumByList)
            {
                if (term != "0")
                {
                    // tempFirst only used to allow only one sumBy  
                    if (tempFirst)
                    {
                        if (sqlGroupBy.Count == 1)
                        {
                            retStr = retStr + " WHERE T1.DataId = " + term + " GROUP BY " + sqlStart;
                        }
                        else
                        {
                            retStr = retStr + " AND T1.DataId = " + term + " GROUP BY " + sqlStart;
                        }
                        sqlStart = sqlStart + ", SUM(T1.DataValue) ";
                        tempFirst = false;
                    }
                }


            }

            //foreach (var term in groupByList)
            //{
            //    sqlStart = 
            //}


            if (retStr != "")
            {
                retStr = "SELECT DISTINCT " + sqlStart + retStr;
            }

            return retStr;
        }

        private string GetPropName(int propId)
        {
            PropName myProperty = new PropName();
            myProperty.PropertyName = db.PropertyName.Find(propId).PropertyName;

            return myProperty.PropertyName;
        }

        public string GetSqlCondition()
        {
            string sqlStr = "", cWhere = "", locSelection, tempWhere, sqlStart = "SELECT ";
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

        // GET: AnalysisResult/Details/5
        public ActionResult Analyze()
        {
            var noSel = Request.Form["noSelections"];
            return View();
        }

        // GET: AnalysisResult/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AnalysisResult/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: AnalysisResult/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: AnalysisResult/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: AnalysisResult/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: AnalysisResult/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
