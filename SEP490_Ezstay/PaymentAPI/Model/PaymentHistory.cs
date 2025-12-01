// using MongoDB.Bson;
// using MongoDB.Bson.Serialization.Attributes;
// using Shared.Enums;
//
// namespace PaymentAPI.Model;
//
// public class PaymentHistory
// {
//     [BsonId]
//     [BsonGuidRepresentation(GuidRepresentation.Standard)]
//     public Guid Id { get; set; } = Guid.NewGuid();
//     
//     [BsonGuidRepresentation(GuidRepresentation.Standard)]
//     public Guid PaymentId { get; set; }
//     
//     [BsonGuidRepresentation(GuidRepresentation.Standard)]
//     public Guid UtilityBillId { get; set; }
//     
//     // Transaction info from SePay webhook
//     public string? SePayTransactionId { get; set; }
//     public decimal Amount { get; set; }
//     public string? TransactionContent { get; set; }
//     public string? AccountNumber { get; set; }
//     public string? Gateway { get; set; } // Tên ngân hàng
//     public string? TransferType { get; set; } // in/out
//     public string? ReferenceCode { get; set; }
//     
//     // Status change
//     [BsonRepresentation(BsonType.String)]
//     public PaymentStatus PreviousStatus { get; set; }
//     
//     [BsonRepresentation(BsonType.String)]
//     public PaymentStatus NewStatus { get; set; }
//     
//     public string? StatusChangeReason { get; set; }
//     
//     // Metadata
//     public DateTime TransactionDate { get; set; }
//     public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
//     
//     [BsonGuidRepresentation(GuidRepresentation.Standard)]
//     public Guid? ChangedBy { get; set; }
//     
//     // Raw webhook data for debugging
//     public string? RawWebhookData { get; set; }
// }
