using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace LAnalyzer.Models
{
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }
        public string UserId { get; set; }
        public string ProjectName { get; set; }

        public void Delete_Project(int projectID)
        {

        }
    }

        public class PropRow
    {
        [Key]
        public int Id { get; set; }
        public int RowId { get; set; }
        public int PropertyValueId { get; set; }
    }

    public class PropName
    {
        [Key]
        public int PropertyId { get; set; }
        public int ProjectId { get; set; }
        public string PropertyName { get; set; }

        public void DeletePropName(int propertyId)
        {

        }
    }

    public class PropValue
    {
        [Key]
        public int PropertyValueId { get; set; }
        public int PropertyId { get; set; }
        public string PropertyValue { get; set; }
    }

    public class DataRow
    {
        [Key]
        public int Id { get; set; }
        public int RowId { get; set; }
        public int DataId { get; set; }
        public double DataValue { get; set; }
    }

    public class DataName
    {
        [Key]
        public int DataId { get; set; }
        public int ProjectId { get; set; }
        public string Data_Name { get; set; }
    }


}