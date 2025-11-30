using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Shared.Enums;

namespace UtilityBillAPI.Models
{
    public class UtilityBillDetail
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }
        public string Type { get; set; } = null!;
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid UtilityBillId { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? PreviousIndex { get; set; }
        public decimal? CurrentIndex { get; set; }
        public decimal? Consumption { get; set; }
        public string? ServiceName { get; set; }
        public decimal? ServicePrice { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Total { get; set; }
    }
}
