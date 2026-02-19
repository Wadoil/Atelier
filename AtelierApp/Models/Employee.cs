using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierApp.Models
{
    internal class Employee
    {
        [Key]
        public int ID { get; set; }

        public DateTime EmploymentDate { get; set; }
        public decimal Salary { get; set; }

        public int UserID { get; set; }
        public int PositionID { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; }
        [ForeignKey("PositionID")]
        public virtual Position Position { get; set; }
    }
}
