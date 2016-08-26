using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Goodhue.Models
{
    public class Department
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }
    public class DepartmentDBContext : DbContext
    {
        public DbSet<Department> Departments { get; set; }
    }
}