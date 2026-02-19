using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierApp.Models
{
    internal class Materials
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Article {  get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [ForeignKey("CategoryID")]
        public int CategoryID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Color { get; set; }

        [ForeignKey("UnitOfMeasurementID")]
        public int UnitOfMeasurementID { get; set; }

        public decimal CurrentAmount { get; set; }
        public decimal MinimalAmountForReplenishment { get; set; }
        public decimal Price { get; set; }

        [ForeignKey("SupplierID")]
        public int SupplierID { get; set; }

        public virtual MaterialCategories Category { get; set; }
        public virtual Measurement UnitOfMeasurement { get; set; }
        public virtual Supplier Supplier { get; set; }
    }
}
