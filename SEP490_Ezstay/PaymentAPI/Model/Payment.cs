using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Shared.Enums;

namespace PaymentAPI.Model;

public class Payment
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid BillId { get; set; }
    
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid TenantId { get; set; }
    
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid OwnerId { get; set; }
    public string TransactionId { get; set; }
    public decimal TransferAmount { get; set; }
    public string Content { get; set; }
    public string AccountNumber { get; set; }
    public string Gateway { get; set; }
    public string TransferType { get; set; } // in/out
    public DateTime TransactionDate { get; set; }
}