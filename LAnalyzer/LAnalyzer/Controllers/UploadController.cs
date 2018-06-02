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
    // Controller for upload of file from client to database on server
    {
        //DB_Context context = new DB_Context();

        public ActionResult UploadDocument(string project = "", string uploadFile = "")
        // Uploads file from client to server - uses async task
        {
            // Check that Project Name is set, if so save it to database
            if (project != "")
            {
                // Get UrserId if logged on
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
                // return with the action to show file content on screen
                return RedirectToAction("FileParser", new { project = project, csvFile = uploadFile });
                //return View("../Home/Index");
            }
            else
            {
                return View();
            }
        }

        [HttpPost]
        //public async Task<ActionResult> Upload()
        public ActionResult Upload()
        // Saves transferred file locally on server before upload to database
        {
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
                    // Go to action to show file content on screen
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
            // Checks that the project does not exist already
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
        // Parses the uploaded file to a CSVFile object containing alll necessary data from the file
        {     
            var nameList = new List<string>();
            var valueList = new List<List<string>>();
            var typeList = new List<string>();
            var path = Path.Combine(Server.MapPath("~/App_Data/Files/"), csvFile);
            var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(path, System.Text.Encoding.UTF7);
            parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
            // delimiter set to ; - this is where we could change code to handle other delimiters as well
            parser.SetDelimiters(new string[] { ";" });
            int i = 0;
            // Parse the file content
            while (!parser.EndOfData)
            {
                // Get one row of data
                string[] row = parser.ReadFields();
                // Get the column names
                if (i == 0) nameList = row.ToList();
                else  // get file content
                {
                    valueList.Add(row.ToList());
                    if (i == 1)
                    {
                        // Define types of the elements of the first row with data
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
                        // Check the types of the eleents of the following rows, change to S (string) if we detect one
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
            // Create CSVFile object, give the properties values and return
            CSVFile myFile = new CSVFile();
            myFile.Project = project;
            myFile.NameList = nameList;
            myFile.ValueList = valueList;
            myFile.TypeList = typeList;
            return myFile;
        }



        bool IsDecimalFormat(string input)
        {
            Decimal dummy;
            return Decimal.TryParse(input, out dummy);
        }


        public ActionResult Save_File_Data(string project, string fileName)
        // Save transferred File to database
        {
            // Use ADO instead of EF due to performance issues

            string ADOcon = ConfigurationManager.ConnectionStrings["ADOLucroAnalyzer"].ConnectionString;
            SqlConnection DbConnection = new SqlConnection(ADOcon);
            DbConnection.Open();

            DB_Context db = new DB_Context();
            CSVFile myData = new CSVFile();
            myData = UploadToObject(project, fileName);

            var projectList = db.Project.ToList();

            // Get the ProjectId for the Project Name given.

            int myProjectId = (from projItem in projectList
                               where projItem.ProjectName == project && projItem.UserId == User.Identity.GetUserId()
                               select projItem.ProjectId).FirstOrDefault();

            // Save the Properties and DataNames in the database and return their corresponding Id's (key values9 from the database model
            List<List<int>> myIDList = FillNames(myProjectId, myData.NameList, myData.TypeList);

            ProjectData myProject = new ProjectData();

            // SHow that the project is in the update status (used to show a text in the project list oin the View)
            myProject.SetProjectstatus(myProjectId, "U");

            // Fill all values for properties and datanames in the database 
            myProject.FillValues(ADOcon, context: db, myProjectID: myProjectId, myIDList: myIDList, myTypeList: myData.TypeList, myValueList: myData.ValueList);

            db.SaveChanges();
            db.Dispose();
            DbConnection.Close();

            // Show project List
            return RedirectToAction("../Projects/ProjectList");
            
        }

        private List<List<int>> FillNames(int projectId, List<string> namesList, List<string> typeList)
        // Fill all the names of Properties and Data from the transferred file and returns their corresponding id's (key values)
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

            // Fill property and data names in database
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

            // Get the id's (key values) for the Properties of this project
            var myPropList = from names in context.PropertyName where names.ProjectId == projectId select names.PropertyId;
            foreach (var item in myPropList)
            {
                myPropIdList.Add(item);
            }

            // Get the id's (key values) for the Data Names of this project
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

    }
}