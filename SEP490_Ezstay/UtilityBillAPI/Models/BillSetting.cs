using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UtilityBillAPI.Models
{
    public class BillSetting
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid OnwerId { get; set; }
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid? HouseId { get; set; }
        public int GenerateDay { get; set; }          
        public bool IsAutoGenerateEnabled { get; set; }
    }
}
