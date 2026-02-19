using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierApp.Models
{
    internal class OrderEmployee
    {
        [Key]
        public int ID { get; set; }

        [ForeignKey("OrderID")]
        public int OrderID { get; set; }

        [ForeignKey("EmployeeID")]
        public int EmployeeID { get; set; }

        public virtual Employee Employee { get; set; }
        public virtual Order Order { get; set; }
    }
}
