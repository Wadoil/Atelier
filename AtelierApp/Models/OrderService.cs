using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierApp.Models
{
    internal class OrderService
    {
        [Key]
        public int ID { get; set; }

        [ForeignKey("OrderID")]
        public int OrderID { get; set; }

        [ForeignKey("OrderID")]
        public int ServiceID { get; set; }

        public virtual Order Order { get; set; }
        public virtual Service Service { get; set; }
    }
}
