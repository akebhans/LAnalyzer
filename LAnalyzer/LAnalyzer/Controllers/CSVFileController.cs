using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using LAnalyzer.Context;
using LAnalyzer.Models;

namespace LAnalyzer.Controllers
{
    public class CSVFileController : Controller
    {
        private DB_Context db = new DB_Context();
        // GET: CSVFile
        public ActionResult Index()
        {
            return View();
        }

        //public void Save_File_Data(CSVFile myFile)
        //public void Save_File_Data(string project, List<string> nameList, List<object> valueList, List<string> typeList)
        //{

        //}

        public ActionResult Save_File_Data(string project, List<string> nameList, List<object> valueList, List<string> typeList)
        {
            return View();
        }
    }
}