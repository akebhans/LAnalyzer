using LAnalyzer.Context;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace LAnalyzer.Models
{
    public class ProjectData : Controller
    {
        public async Task<ActionResult> FillValues(string ADOcon, DB_Context context, int myProjectID, List<List<int>> myIDList, List<string> myTypeList, List<List<string>> myValueList)
        {
            ViewBag.Message = "Upload of project data to database is ongoing...";
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
                ProjectData myProject = new ProjectData();

                myProject.SetProjectstatus(myProjectID, "");

                // Upload to Database is completed - trig event to be able to send message to user

                MessagePublisher myMessagePublisher = new MessagePublisher();
                MessageSubscriber myMessageSubScriber = new MessageSubscriber();
                myMessagePublisher.messageEvent += myMessageSubScriber.HandleMessage;

                MsgArgClass myMsg = new MsgArgClass("Database updated!");

                myMessagePublisher.TrigEvent(myMsg);

            });

            return View("UploadDocument");

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


        }

        public async Task<ActionResult> DeleteProject(int id)
        {
            SetProjectstatus(id, "D");
            string ADOcon = ConfigurationManager.ConnectionStrings["ADOLucroAnalyzer"].ConnectionString;
            DB_Context context = new DB_Context();
            TempData["shortMessage"] = "Project deletion is ongoing...";
            await Task.Run(() =>
                {
                    // Use ADO instead of EF due to performance issues
                    
                    // DELETE Project

                    SqlConnection dbCon = new SqlConnection(ADOcon);
                    dbCon.Open();

                    string sqlDelProject = "DELETE FROM Projects WHERE PROJECTID =" + id.ToString();
                    SqlCommand delProjectCommand = new SqlCommand(sqlDelProject, dbCon);
                    int nRows = delProjectCommand.ExecuteNonQuery();

                    // DELETE  records in PropRows 

                    string sqlDelPropRows = "DELETE A FROM PROPROWS A JOIN  PROPVALUES B ON A.PROPERTYVALUEID = B.PROPERTYVALUEID " +
                    "JOIN PROPNAMES C ON B.PROPERTYID = C.PROPERTYID WHERE C.PROJECTID = " + id.ToString();

                    SqlCommand delPropRowsCommand = new SqlCommand(sqlDelPropRows, dbCon);
                    nRows = delPropRowsCommand.ExecuteNonQuery();

                    // DELETE  records in PropValues 

                    string sqlDelPropValues = "DELETE A FROM PROPVALUES A JOIN  PROPNAMES B ON A.PROPERTYID = B.PROPERTYID " +
                    "WHERE B.PROJECTID = " + id.ToString();

                    SqlCommand delPropValuesCommand = new SqlCommand(sqlDelPropValues, dbCon);
                    nRows = delPropValuesCommand.ExecuteNonQuery();

                    //            DELETE records in PropNames

                    string sqlDelPropNames = "DELETE FROM PROPNAMES WHERE PROJECTID = " + id.ToString();

                    SqlCommand delPropNamesCommand = new SqlCommand(sqlDelPropNames, dbCon);
                    nRows = delPropNamesCommand.ExecuteNonQuery();

                    // DELETE records in DataRow

                    string sqlDelDataRows = "DELETE A FROM DATAROWS A JOIN DATANAMES B ON A.DATAID = B.DATAID WHERE B.PROJECTID = " + id.ToString();

                    SqlCommand delDataRowsCommand = new SqlCommand(sqlDelDataRows, dbCon);
                    nRows = delDataRowsCommand.ExecuteNonQuery();

                    // DELETE records in DataNames

                    string sqlDelDataNames = "DELETE FROM DATANAMES WHERE PROJECTID = " + id.ToString();

                    SqlCommand delDataNamesCommand = new SqlCommand(sqlDelDataNames, dbCon);
                    nRows = delDataNamesCommand.ExecuteNonQuery();

                    dbCon.Close();
                });

            return RedirectToAction("ProjectList");
        }

        public void SetProjectstatus(int projId, string myStatus)
        {
            DB_Context context = new DB_Context();
            Project project = context.Project.Find(projId);
            project.Status = myStatus;
            try
            {
                context.SaveChanges();
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException e)
            {
                // DBUpdate error
                TempData["shortMessage"] = "DB Update failed!!";
            }
            context.Dispose();
        }
        public class MessagePublisher
        {
            public EventHandler<MsgArgClass> messageEvent;

            public void TrigEvent(MsgArgClass msg)
            {
                messageEvent(this, msg);
            }
        }

        public class MessageSubscriber
        {
            public void HandleMessage(object sender, MsgArgClass msg)
            {
                //Trigga i foreground när DB är uppdaterad!!

                // Would like to pass message to foreground task - but how?
            }

        }

         public class MsgArgClass : EventArgs
        {
            public string message { get; }

            public MsgArgClass(string msg)
            {
                message = msg;
            }
        }
    }

}
