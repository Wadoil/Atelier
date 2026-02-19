using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierApp.Models
{
    internal class Order
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int ClientID { get; set; }

        public string Description { get; set; }

        [Required]
        public int StatusID { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Cost { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Prepayment { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateOfFitting { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateOfReadiness { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateOfIssue { get; set; }

        // Навигационные свойства
        [ForeignKey("ClientID")]
        public virtual Client Client { get; set; }

        [ForeignKey("StatusID")]
        public virtual Status Status { get; set; }

        public virtual ICollection<OrderService> OrderServices { get; set; }
        public virtual ICollection<UsedMaterial> UsedMaterials { get; set; }
        public virtual ICollection<OrderEmployee> OrderEmployees { get; set; }
    }
}
