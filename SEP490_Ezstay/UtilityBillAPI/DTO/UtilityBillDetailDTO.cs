
namespace UtilityBillAPI.DTO
{
    public class UtilityBillDetailDTO
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = null!;
        public Guid UtilityBillId { get; set; }
        // Only for UtilityReading
        public decimal? UnitPrice { get; set; }
        public decimal? PreviousIndex { get; set; }
        public decimal? CurrentIndex { get; set; }
        public decimal? Consumption { get; set; }
        // Only for Service
        public string? ServiceName { get; set; }
        public decimal? ServicePrice { get; set; }

        public decimal Total { get; set; }
    }
}
