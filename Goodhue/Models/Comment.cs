using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Goodhue.Models
{
    public class Comment
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public int CarId { get; set; }
        [Display(Name = "Comment")]
        public string Text { get; set; }
    }
    public class CommentDBContext : DbContext
    {
        public DbSet<Comment> Comments { get; set; }
    }
}