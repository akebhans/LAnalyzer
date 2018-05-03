using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using LAnalyzer.Context;
using LAnalyzer.Models;
using Microsoft.AspNet.Identity;
using System.Configuration;

namespace LAnalyzer.Controllers
{
    public class ProjectsController : Controller
    {
        private DB_Context db = new DB_Context();

        // GET: Projects
        public ActionResult ProjectList()
        {
            string myId = User.Identity.GetUserId();
            var projectList = db.Project.ToList();
            List<Project> userProjectList = new List<Project>();

            foreach (var item in projectList)
            {
                if (item.UserId == myId)
                {
                    userProjectList.Add(item);
                }
            }

            return View(userProjectList);
        }



        // GET: Projects/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Project.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // GET: Projects/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Projects/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ProjectId,UserId,ProjectName")] Project project)
        {
            if (ModelState.IsValid)
            {
                db.Project.Add(project);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(project);
        }

        // GET: Projects/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Project.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ProjectId,UserId,ProjectName")] Project project)
        {
            if (ModelState.IsValid)
            {
                db.Entry(project).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(project);
        }

        // GET: Projects/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Project.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // DELETE Project            
            Project project = db.Project.Find(id);
            db.Project.Remove(project);

            List<PropRow> propRowList = db.PropertyRow.ToList();
            List<PropValue> propValueList = db.PropertyValue.ToList();
            List<PropName> propNameList = db.PropertyName.ToList();

            // DELETE  records in PropRows 

            //string deletePropRows = "DELETE FROM PropertyRow WHERE PropertyValueId In (Select"

            var deletePropRows =
                from rows in db.PropertyRow
                join values in db.PropertyValue on rows.PropertyValueId equals values.PropertyValueId
                join names in db.PropertyName on values.PropertyId equals names.PropertyId
                where names.ProjectId == id
                select rows;

            foreach (var item in deletePropRows)
            {
                db.PropertyRow.Remove(item);
            }

            // DELETE  records in PropValues 

            //var deletePropValues =
            //    from values in db.PropertyValue join names in db.PropertyName on values.PropertyId equals names.PropertyId 
            //    where names.ProjectId == id
            //    select values;

            //string deletePropValues = "DELETE FROM PropertyValue WHERE PropertyId IN (SELECT PropertyId FROM PropertyName WHERE ProjectId = " + id.ToString();

            //using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["LucroAnalyzer"].ConnectionString))
            //{
            //    SqlCommand command = new SqlCommand(deletePropValues, con);
            //    try
            //    {
            //        con.Open();
            //        command.ExecuteNonQuery();
            //    }
            //    catch
            //    {

            //    }
            //}

            //var test = from names in db.PropertyName where names.ProjectId == id select names.PropertyId;
            //foreach (var item in test)
            //{
            //    var test1 = from values in db.PropertyValue where values.PropertyId == item select values.PropertyValueId;
            //    foreach (var row in test1)
            //    {
            //        var deletePropValues =
            //          from values in db.PropertyValue
            //          where values.PropertyValueId == row
            //          select values;

            //        foreach (var line in deletePropValues)
            //        {
            //            db.PropertyValue.Remove(line);
            //        }
            //    }
            //}


            var deletePropValues =
            from values in db.PropertyValue
            join names in db.PropertyName on values.PropertyId equals names.PropertyId
            where names.ProjectId == id
            select values;

            foreach (var item in deletePropValues)
            {
                db.PropertyValue.Remove(item);
            }

            //            DELETE records in PropNames

            var deletePropNames =
                  from names in db.PropertyName
                  where names.ProjectId == id
                  select names;

            foreach (var item in deletePropNames)
            {
                db.PropertyName.Remove(item);
            }


            List<Models.DataRow> dataRowList = db.DataRow.ToList();
            List<DataName> dataNameList = db.DataName.ToList();

            // DELETE records in DataRow



            var deleteDataRows =
                from rows in db.DataRow
                join names in db.DataName on rows.DataId equals names.DataId
                where names.ProjectId == id
                select rows;

            foreach (var item in deleteDataRows)
            {
                db.DataRow.Remove(item);
            }

            // DELETE records in DataNames

            var deleteDataNames =
                from names in db.DataName
                where names.ProjectId == id
                select names;

            foreach (var item in deleteDataNames)
            {
                db.DataName.Remove(item);
            }

            db.SaveChanges();
            db.Dispose();

            return RedirectToAction("ProjectList");
            //return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
