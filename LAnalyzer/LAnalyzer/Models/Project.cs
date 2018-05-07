using LAnalyzer.Context;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace LAnalyzer.Models
{
    public class ProjectData : Controller
    {

        public async Task FillValues(string ADOcon, DB_Context context, int myProjectID, List<List<int>> myIDList, List<string> myTypeList, List<List<string>> myValueList)
        {
            await Task.Run(() =>
            {
                SqlConnection DBcon = new SqlConnection(ADOcon);
                DBcon.Open();
                int LastRow = 0, lastPropertyIndex, lastDataIndex;

                // For each row...

                foreach (List<string> row in myValueList)
                {
                    lastPropertyIndex = 0;
                    lastDataIndex = 0;

                    // For each column

                    int lastColumn = 0;

                    foreach (string column in row)
                    {
                        if (myTypeList[lastColumn] == "S")
                        {
                            SavePropertyValue(DBcon, context, myIDList[0][lastPropertyIndex], column, LastRow);
                            lastPropertyIndex++;
                        }
                        else if (myTypeList[lastColumn] == "N")
                        {
                            SaveDataValue(DBcon, context, myIDList[1][lastDataIndex], column, LastRow);
                            lastDataIndex++;
                        }
                        else
                        {

                        }
                        lastColumn++;
                        // RÄKNA UPP lastColumn etc...
                    }
                    LastRow++;

                }
                ProjectFinished();
            });

            void SavePropertyValue(SqlConnection sqlCon, DB_Context loccontext, int myPropertyId, string myPropertyValue, int rowNo)
            {

                myPropertyValue = myPropertyValue.Replace("'", "''");

                string sqlPropValue = "INSERT INTO PROPVALUES (PROPERTYID, PROPERTYVALUE) SELECT " + myPropertyId.ToString() + ", '" +
                    myPropertyValue + "' WHERE NOT EXISTS (SELECT * FROM PropValues WHERE PropertyValue = '" + myPropertyValue + "' AND PROPERTYID = " +
                    myPropertyId.ToString() + ")";

                SqlCommand addPropVCommand = new SqlCommand(sqlPropValue, sqlCon);

                int nRows = addPropVCommand.ExecuteNonQuery();

                string sqlGetPropValueId = "SELECT PROPERTYVALUEID FROM PROPVALUES  WHERE PropertyValue = '" + myPropertyValue + "' AND PROPERTYID = " +
                    myPropertyId.ToString();

                string sqlPropRow = "INSERT INTO PROPROWS (ROWID, PROPERTYVALUEID) VALUES (" + rowNo.ToString() + ",(" + sqlGetPropValueId + "))";

                SqlCommand addPropRCommand = new SqlCommand(sqlPropRow, sqlCon);


                nRows = addPropRCommand.ExecuteNonQuery();


            }

            void SaveDataValue(SqlConnection sqlCon, DB_Context loccontext, int myDataId, string myDataValue, int rowNo)
            {
                myDataValue = myDataValue.Replace(",", ".");

                string addDataRSql = "INSERT INTO DATAROWS (ROWID, DATAID, DATAVALUE) VALUES (" + rowNo.ToString() + "," + myDataId + "," + myDataValue + ")";

                SqlCommand addDataRCommand = new SqlCommand(addDataRSql, sqlCon);

                addDataRCommand.ExecuteNonQuery();

            }
            ActionResult ProjectFinished()
            {
                ViewBag.Message = "HEJ";
                return View();

            }
        }

        //public async Task RemoveProject()
    }
}
