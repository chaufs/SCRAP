using System;
using System.Collections.Generic;
using System.Text;

namespace SCRAP.domain.entities
{
    public class Product
    {
        public int ProductId { get; set; }



        public string ProductCode { get; set; } = string.Empty;



        public string ProductName { get; set; } = string.Empty;



        public decimal UnitPrice { get; set; }



        public bool IsActive { get; set; } = true;



        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    }
}
