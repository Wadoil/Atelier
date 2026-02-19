using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace AtelierApp.Models
{
    internal class UsedMaterial
    {
        [Key]
        public int ID { get; set; }

        [ForeignKey("OrderID")]
        public int OrderID { get; set; }

        [ForeignKey("MaterialID")]
        public int MaterialID { get; set; } 
        public decimal Quantity { get; set; }

        public virtual Order Order { get; set; }
        public virtual Materials Material { get; set; }
    }
}
