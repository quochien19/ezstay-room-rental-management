using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Shared.Enums;

namespace UtilityBillAPI.Models
{
    [BsonIgnoreExtraElements]
    public class UtilityBill
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid OwnerId { get; set; }
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid TenantId { get; set; }
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid ContractId { get; set; }              
        public string HouseName { get; set; } = null!;
        public string RoomName { get; set; } = null!;
        public decimal? RoomPrice { get; set; }
        public List<UtilityBillDetail> Details { get; set; } = new();        
        public decimal TotalAmount { get; set; }       
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedAt { get; set; }
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime? UpdatedAt { get; set; }
        [BsonRepresentation(BsonType.String)]
        public UtilityBillType BillType { get; set; }
        [BsonRepresentation(BsonType.String)]
        public UtilityBillStatus Status { get; set; }
        public string? Note { get; set; }
        public string? Reason { get; set; }

    }
}
