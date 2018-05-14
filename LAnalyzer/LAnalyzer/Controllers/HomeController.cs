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

namespace LAnalyzer.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            //if (Request["FirstTime"] == null)
            //{
            
            //    DB_Context db = new DB_Context();
            //    var projectList = db.Project.ToList();

            //    foreach (var item in projectList)
            //    {
            //        item.Status = "";
            //    }
            //    db.SaveChanges();
            //    db.Dispose();
            //}
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult TEST()
        {
            return View();
        }


    }

    public class OLDUploadController : Controller
    {
        public ActionResult UploadDocument(string project="")
        {
            if (project != "")
            {
                string myId = User.Identity.GetUserId();
                DB_Context context = new DB_Context();
                Project myProject = new Project();
                myProject.ProjectId = 1;
                myProject.ProjectName = project;
                context.Project.Add(myProject);
                context.SaveChanges();
                return View("../Home/Index");
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

                if (file != null && file.ContentLength > 0)
                {
                    var tempPath = Path.GetTempPath();
                    var fileName = Path.GetFileName(file.FileName);

                    var path = Path.Combine(Server.MapPath("~/App_Data/Files/"), fileName);

                    file.SaveAs(path);
                }
                if (myProject != "")
                {
                    return RedirectToAction("UploadDocument", new { project = myProject });
                }
                else
                {
                    return RedirectToAction("UploadDocument");
                }
            }

            return RedirectToAction("UploadDocument");

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