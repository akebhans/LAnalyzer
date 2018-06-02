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


namespace LAnalyzer.Controllers
{
    public class ProjectsController : Controller
    {
        private DB_Context db = new DB_Context();

        // GET: Projects
        public ActionResult ProjectList()
        {
            // Refresh to get updates from actions lika upload/deletion of projects.
            this.HttpContext.Response.AddHeader("refresh", "5; url=" + Url.Action("ProjectList"));

            if (TempData["shortMessage"] != null)
            {
                ViewBag.Message = TempData["shortMessage"].ToString();
            }
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
            ProjectData myProject = new ProjectData();
            myProject.DeleteProject(id);

            ViewBag.Message = "Deletion of project is ongoing in the background...";

            return RedirectToAction("ProjectList");
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
