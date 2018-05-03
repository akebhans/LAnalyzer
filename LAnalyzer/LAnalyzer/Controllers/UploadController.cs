using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LAnalyzer.Models;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using LAnalyzer.Context;
using Microsoft.VisualBasic.FileIO;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections;
using System.Data.SqlClient;
using System.Configuration;

namespace LAnalyzer.Controllers
{
    public class UploadController : Controller
    {
        //DB_Context context = new DB_Context();

        public ActionResult UploadDocument(string project = "", string uploadFile = "")
        {
            if (project != "")
            {

                string myId = User.Identity.GetUserId();
                if (CheckExistProject(myId, project))
                {
                    ViewBag.Message = "Project " + project + " exists already. Choose another name!";
                    return View();
                }
                DB_Context context = new DB_Context();
                Project myProject = new Project();
                myProject.ProjectId = 1;
                myProject.UserId = myId;
                myProject.ProjectName = project;
                context.Project.Add(myProject);
                context.SaveChanges();
                context.Dispose();
                //var path = Path.Combine(Server.MapPath("~/App_Data/Files/"), uploadFile);
                //var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(path);
                //parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                //parser.SetDelimiters(new string[] { ";" });

                //while (!parser.EndOfData)
                //{
                //    string[] row = parser.ReadFields();
                //    /* do something */
                //}
                //CSVFile myCSVFile = new CSVFile();
                //myCSVFile = FileParser(uploadFile);
                //return RedirectToAction("UploadDocument", new { project = myProject, uploadFile = fileName });
                return RedirectToAction("FileParser", new { project = project, csvFile = uploadFile });
                //return View("../Home/Index");
            }
            else
            {
                string myId = User.Identity.GetUserId();
                string test = Request.Form["Project"];
                return View();
            }
        }

        [HttpPost]
        //public async Task<ActionResult> Upload()
        public ActionResult Upload()
        {
            //Request.Form[]
            string myProject = Request.Form["Project"];
            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];

                string fileName = "";

                if (file != null && file.ContentLength > 0)
                {
                    var tempPath = Path.GetTempPath();
                    fileName = Path.GetFileName(file.FileName);

                    var path = Path.Combine(Server.MapPath("~/App_Data/Files/"), fileName);

                    file.SaveAs(path);
                }
                if (myProject != "")
                {
                    return RedirectToAction("UploadDocument", new { project = myProject, uploadFile = fileName });
                }
                else
                {
                    return RedirectToAction("UploadDocument");
                }
            }

            return RedirectToAction("UploadDocument");

        }

        public bool CheckExistProject(string user, string project)
        {
            DB_Context context = new DB_Context();
            using (context)
            {
                var projects = from p in context.Project
                               where p.ProjectName == project && p.UserId == user
                               select p;
                Dispose();
                if (projects.Count() > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }

        }

        public ActionResult FileParser(string project, string csvFile)
        {
            ViewBag.Content = csvFile;
            return View(UploadToObject(project, csvFile));
        }

        public CSVFile UploadToObject(string project, string csvFile)
        {
            var nameList = new List<string>();
            var valueList = new List<List<string>>();
            var typeList = new List<string>();
            var path = Path.Combine(Server.MapPath("~/App_Data/Files/"), csvFile);
            var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(path);
            parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
            parser.SetDelimiters(new string[] { ";" });
            int i = 0;
            while (!parser.EndOfData)
            {
                string[] row = parser.ReadFields();
                if (i == 0) nameList = row.ToList();
                else
                {
                    valueList.Add(row.ToList());
                    if (i == 1)
                    {
                        typeList = row.ToList();
                        int j = 0;
                        foreach (var term in row)
                        {
                            if (double.TryParse(term, out double n)) typeList[j] = "N";
                            else typeList[j] = "S";
                            j++;
                        }
                    }
                    else
                    {
                        int j = 0;
                        foreach (var term in row)
                        {
                            if (!double.TryParse(term, out double n)) typeList[j] = "S";
                            j++;
                        }

                    }
                }
                i++;
            }
            CSVFile myFile = new CSVFile();
            myFile.Project = project;
            myFile.NameList = nameList;
            myFile.ValueList = valueList;
            myFile.TypeList = typeList;
            return myFile;
        }


        public string CsvToJson(string myFile)
        {
            var tempPath = Path.GetTempPath();
            var csv = new List<string[]>();
            var lines = System.IO.File.ReadAllLines(tempPath + "/" + myFile);
            int i = 0;
            string[] jsonElement = new string[2];
            List<string[]> jsonLine = new List<string[]>();
            string[] colElements = null, dataElements = null;
            string jsonString = "";
            foreach (string line in lines)
            {
                if (i == 0)
                {
                    colElements = line.Split(';').ToArray();
                }
                else
                {
                    dataElements = line.Split(';').ToArray();
                    jsonString += "{";
                    for (int j = 0; j < colElements.Length; j++)
                    {
                        if (IsDecimalFormat(dataElements[j]))
                        {
                            jsonString += "\"" + colElements[j] + "\":" + dataElements[j];
                        }
                        else
                        {
                            jsonString += "\"" + colElements[j] + "\":\"" + dataElements[j] + "\"";
                        }
                        if (j < colElements.Length - 1) jsonString += ",";
                    }
                    jsonString += "}";
                }

                i++;
            }
            return jsonString;
        }

        bool IsDecimalFormat(string input)
        {
            Decimal dummy;
            return Decimal.TryParse(input, out dummy);
        }

        public ActionResult Save_File_Data(string project, string fileName)
        {
            // Use ADO instead of EF due to performance issues

            string ADOcon = ConfigurationManager.ConnectionStrings["ADOLucroAnalyzer"].ConnectionString;
            SqlConnection DbConnection = new SqlConnection(ADOcon);
            DbConnection.Open();

            DB_Context db = new DB_Context();
            CSVFile myData = new CSVFile();
            myData = UploadToObject(project, fileName);

            var projectList = db.Project.ToList();

            int myProjectId = (from projItem in projectList
                               where projItem.ProjectName == project && projItem.UserId == User.Identity.GetUserId()
                               select projItem.ProjectId).FirstOrDefault();





            List<List<int>> myIDList = FillNames(myProjectId, myData.NameList, myData.TypeList);

            FillValues(DbConnection,db,myProjectId, myIDList, myData.TypeList, myData.ValueList);

            // Databas-anrop HÄR!!!
            db.SaveChanges();
            db.Dispose();
            DbConnection.Close();
            return View();
        }

        private List<List<int>> FillNames(int projectId, List<string> namesList, List<string> typeList)
        {
            DB_Context context = new DB_Context();

            // Initiate for table PropNames
            var namePropList = context.PropertyName.ToList();
            int lastPropertyId;
            // Get last PropertyId in PropertyName
            try
            {
                lastPropertyId = (from nameItem in namePropList
                                  orderby nameItem.PropertyId
                                  select nameItem.PropertyId).Last();
            }
            catch (InvalidOperationException e)
            {
                // if table PropertyName is empty set lastPropertyId = 0
                lastPropertyId = 0;
            }

            // Initiate for table DataNames
            var nameDataList = context.DataName.ToList();
            int lastDataId;
            // Get last DataId in DataNames
            try
            {
                lastDataId = (from nameItem in nameDataList
                              orderby nameItem.DataId
                              select nameItem.DataId).Last();
            }
            catch (InvalidOperationException e)
            {
                // if table DataNames is empty set lastDatId = 0
                lastDataId = 0;

            }

            List<int> myPropIdList = new List<int>();
            List<int> myDataIdList = new List<int>();

            int counter = 0;

            foreach (string item in namesList)
            {
                if (typeList[counter] == "S")
                {
                    PropName myPropName = new PropName();
                    lastPropertyId++;
                    myPropName.PropertyId = lastPropertyId;
                    context.PropertyName.Add(new PropName { ProjectId = projectId, PropertyName = item });
                }
                else
                {
                    DataName myDataName = new DataName();
                    lastDataId++;
                    myDataName.DataId = lastDataId;
                    context.DataName.Add(new DataName { ProjectId = projectId, Data_Name = item });
                }
                counter++;
            }
            context.SaveChanges();
            var myPropList = from names in context.PropertyName where names.ProjectId == projectId select names.PropertyId;
            foreach (var item in myPropList)
            {
                myPropIdList.Add(item);
            }
            var myDataList = from names in context.DataName where names.ProjectId == projectId select names.DataId;
            foreach (var item in myDataList)
            {
                myDataIdList.Add(item);
            }
            context.Dispose();
            List<List<int>> returnList = new List<List<int>>();
            returnList.Add(myPropIdList);
            returnList.Add(myDataIdList);
            return returnList;
        }

        public void FillValues(SqlConnection DBcon,DB_Context context, int myProjectID, List<List<int>> myIDList, List<string> myTypeList, List<List<string>> myValueList)
        {

            // Loop through myValueList


            int LastRow = 0, lastPropertyIndex, lastDataIndex;

            // For each row...

            foreach (List<string> row in myValueList)
            {
                lastPropertyIndex = 0;
                lastDataIndex = 0;

                // For each column

                int lastColumn = 0, myPropertyValueId;

                foreach (string column in row)
                {
                    if (myTypeList[lastColumn] == "S")
                    {
                        //myPropertyValueId = SavePropertyValue(DBcon,context, myIDList[0][lastPropertyIndex], column, LastRow);
                        SavePropertyValue(DBcon, context, myIDList[0][lastPropertyIndex], column, LastRow);
                        lastPropertyIndex++;

                        //SavePropRow

                        // Get last PropRows.Id


                        // Save
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
            ViewBag.Message= "HEJ";
            //int counter = 0, lastPropertyId, lastDataId;

            //foreach (var item in myTypeList)
            //{
            //    // If Property
            //    if (myTypeList[counter] == "S")
            //    {

            //    }
            //}

        }
        //private int SavePropertyValue(SqlConnection sqlCon,DB_Context context,int myPropertyId, string myPropertyValue, int rowNo)
        private void SavePropertyValue(SqlConnection sqlCon, DB_Context context, int myPropertyId, string myPropertyValue, int rowNo)
        {
            //DB_Context context = new DB_Context();

            var propRowsList = context.PropertyRow.ToList();
            var propValueList = context.PropertyValue.ToList();

            // Add PropertyValue

            // Handle eventual apostorphes in PropertyValue

            myPropertyValue = myPropertyValue.Replace("'", "''");

            string sqlPropValue = "INSERT INTO PROPVALUES (PROPERTYID, PROPERTYVALUE) SELECT " + myPropertyId.ToString() + ", '" + 
                myPropertyValue+ "' WHERE NOT EXISTS (SELECT * FROM PropValues WHERE PropertyValue = '" + myPropertyValue + "' AND PROPERTYID = " +
                myPropertyId.ToString() + ")";

            SqlCommand addPropVCommand = new SqlCommand(sqlPropValue, sqlCon);

            int nRows = addPropVCommand.ExecuteNonQuery();

            string sqlGetPropValueId = "SELECT PROPERTYVALUEID FROM PROPVALUES  WHERE PropertyValue = '" + myPropertyValue + "' AND PROPERTYID = " +
                myPropertyId.ToString();

            string sqlPropRow = "INSERT INTO PROPROWS (ROWID, PROPERTYVALUEID) VALUES (" + rowNo.ToString() + ",(" + sqlGetPropValueId + "))";

            SqlCommand addPropRCommand = new SqlCommand(sqlPropRow, sqlCon);


            nRows = addPropRCommand.ExecuteNonQuery();

            //context.PropertyValue.Add(new PropValue { PropertyId = myPropertyId, PropertyValue = myPropertyValue });

            //context.SaveChanges();

            //propRowsList = context.PropertyRow.ToList();

            // get last PropValueId

            //int myPropValueId;

            //try
            //{
            //    myPropValueId = (from propItem in context.PropertyValue 
            //                       orderby propItem.PropertyValueId descending 
            //                       select propItem.PropertyValueId).FirstOrDefault();
            //}
            //catch (InvalidOperationException e)
            //{
            //    // SHOULD NOT HAPPEN - Something is wrong!!
            //    myPropValueId = 0;
            //}


            // Check if exists

            //var myPropValueId = (from propItem in propValueList
            //                     where propItem.PropertyId == myPropertyId &&
            //                     propItem.PropertyValue == myPropertyValue
            //                     select propItem.PropertyValueId).FirstOrDefault();

            //if (myPropValueId == 0)
            //{
            //    int lastPropValueId;
            //    // Get last PropertyId in PropertyName
            //    try
            //    {
            //        lastPropValueId = (from propItem in propValueList
            //                           orderby propItem.PropertyValueId
            //                           select propItem.PropertyValueId).Last();
            //    }
            //    catch (InvalidOperationException e)
            //    {
            //        // if table PropertyName is empty set lastPropertyId = 0
            //        lastPropValueId = 0;
            //    }
            //    myPropValueId = lastPropValueId + 1;
            //    context.PropertyValue.Add(new PropValue { PropertyValueId = myPropValueId, PropertyId = myPropertyId, PropertyValue = myPropertyValue });

            //}

            //context.PropertyRow.Add(new PropRow { PropertyValueId = myPropValueId, RowId = rowNo });

            //context.SaveChanges();

            //context.Dispose();

            //return myPropValueId;

        }

        private void SaveDataValue(SqlConnection sqlCon, DB_Context context,int myDataId, string myDataValue, int rowNo)
        {
            //DB_Context context = new DB_Context();

            //var dataRowsList = context.DataRow.ToList();

            //double myValue;

            //double.TryParse(myDataValue, out myValue);

            //context.DataRow.Add(new DataRow { DataId = myDataId, DataValue = myValue, RowId = rowNo });


            // Decimal comma is replaced with decimal dot
            myDataValue = myDataValue.Replace(",", ".");

            string addDataRSql = "INSERT INTO DATAROWS (ROWID, DATAID, DATAVALUE) VALUES (" + rowNo.ToString() + "," + myDataId + "," + myDataValue + ")";

            SqlCommand addDataRCommand = new SqlCommand(addDataRSql, sqlCon);

            addDataRCommand.ExecuteNonQuery();
            
            //context.SaveChanges();

            //context.Dispose();
        }


        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        context.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}
    }
}