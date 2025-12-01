// using MongoDB.Bson;
// using MongoDB.Bson.Serialization.Attributes;
// using Shared.Enums;
//
// namespace PaymentAPI.Model;
//
// public class Payment
// {
//     [BsonId]
//     [BsonGuidRepresentation(GuidRepresentation.Standard)]
//     public Guid Id { get; set; } = Guid.NewGuid();
//     
//     [BsonGuidRepresentation(GuidRepresentation.Standard)]
//     public Guid UtilityBillId { get; set; }
//     
//     [BsonGuidRepresentation(GuidRepresentation.Standard)]
//     public Guid TenantId { get; set; }
//     
//     [BsonGuidRepresentation(GuidRepresentation.Standard)]
//     public Guid? OwnerId { get; set; }
//     
//     public decimal Amount { get; set; }
//     
//     [BsonRepresentation(BsonType.String)]
//     public PaymentMethod PaymentMethod { get; set; }
//     
//     [BsonRepresentation(BsonType.String)]
//     public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
//     
//     // SePay transaction info
//     public string? TransactionId { get; set; }
//     public string? TransactionContent { get; set; }
//     public string? BankAccountNumber { get; set; }
//     public string? Gateway { get; set; }
//     
//     // QR code info for online payment
//     public string? QrDataUrl { get; set; }
//     public string? PaymentCode { get; set; } // Code nhúng trong nội dung chuyển khoản để định danh
//     
//     // Notes
//     public string? Notes { get; set; }
//     public string? ApprovalNotes { get; set; }
//     
//     // Timestamps
//     public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
//     public DateTime? UpdatedAt { get; set; }
//     public DateTime? PaidAt { get; set; }
//     public DateTime? ApprovedAt { get; set; }
//     
//     [BsonGuidRepresentation(GuidRepresentation.Standard)]
//     public Guid? ApprovedBy { get; set; }
// }


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
    public string TransactionId { get; set; }
    public decimal TransferAmount { get; set; }
    public string Content { get; set; }
    public string AccountNumber { get; set; }
    public string Gateway { get; set; } // Tên ngân hàng
    public string TransferType { get; set; } // in/out
    public DateTime TransactionDate { get; set; }
}