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

namespace LAnalyzer.Controllers
{
    public class UploadController : Controller
    {
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
                return RedirectToAction("FileParser", new { project = project ,csvFile = uploadFile });
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
            var nameList = new List<string>();
            var valueList = new List<object>();
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

            return View(myFile);

            //returnList.Add(nameList);
            //returnList.Add(valueList);
            //returnList.Add(typeList);
            //return returnList;
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
    }
}