using System;

namespace AgrochemOperator.Models
{
    public class ProductionBatch
    {
        public int Id { get; set; }
        public string BatchNumber { get; set; }
        public int OrderId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public decimal ActualQuantityKg { get; set; }
        public string ProductName { get; set; }
    }
}