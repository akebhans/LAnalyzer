using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using LAnalyzer.Models;

namespace LAnalyzer.Context
{
    public partial class DB_Context : DbContext
    {
        public DB_Context() : base("LucroAnalyzer") { }
        public DbSet<Project> Project { get; set; }
        public DbSet<PropRow> PropertyRow { get; set; }
        public DbSet<PropName> PropertyName { get; set; }
        public DbSet<PropValue> PropertyValue { get; set; }
        public DbSet<DataRow> DataRow { get; set; }
        public DbSet<DataName> DataName { get; set; }
    }

}