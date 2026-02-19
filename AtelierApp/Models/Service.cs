using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierApp.Models
{
    internal class Service
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
        public decimal Price { get; set; }
        public ICollection<OrderService> OrderServices { get; set; }
    }
}
