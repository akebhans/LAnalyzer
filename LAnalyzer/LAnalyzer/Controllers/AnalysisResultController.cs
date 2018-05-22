using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using LAnalyzer.Context;
using LAnalyzer.Models;

namespace LAnalyzer.Controllers
{
    public class AnalysisResultController : Controller
    {
        private DB_Context db = new DB_Context();
        // GET: AnalysisResult
        public ActionResult Index(int id)
        {
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

            return View(myResult);

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
        public ActionResult Details(int id)
        {
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
