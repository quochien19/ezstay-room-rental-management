// using PaymentAPI.DTOs.Requests;
// using Shared.DTOs;
// using Shared.Enums;
//
// namespace PaymentAPI.Services.Interfaces;
//
// public interface IPaymentService
// {
//     // Create payment
//     Task<ApiResponse<PaymentResponse>> CreatePaymentAsync(CreatePaymentRequest request, Guid tenantId);
//     Task<ApiResponse<PaymentResponse>> CreateOfflinePaymentAsync(Guid billId, Guid tenantId, string? notes);
//     
//     // QR payment
//     Task<ApiResponse<PaymentQRInfoResponse>> GetPaymentQRInfoAsync(Guid billId, Guid tenantId);
//     
//     // Verify online payment
//     Task<ApiResponse<PaymentResponse>> VerifyOnlinePaymentAsync(VerifyOnlinePaymentRequest request);
//     
//     // Get payment info
//     Task<ApiResponse<PaymentResponse>> GetPaymentByIdAsync(Guid paymentId);
//     Task<ApiResponse<List<PaymentResponse>>> GetPaymentsByBillIdAsync(Guid billId);
//     Task<ApiResponse<List<PaymentResponse>>> GetPaymentsByUserIdAsync(Guid userId);
//     Task<ApiResponse<PaymentResponse>> GetLatestPaymentByBillIdAsync(Guid billId);
//     
//     // Approve offline payment
//     Task<ApiResponse<PaymentResponse>> ApproveOfflinePaymentAsync(Guid paymentId, ApprovePaymentRequest request, Guid ownerId);
//     Task<ApiResponse<List<PaymentResponse>>> GetPendingApprovalsAsync(Guid ownerId);
//     
//     // Check payment status
//     Task<ApiResponse<PaymentStatusResponse>> CheckPaymentStatusAsync(Guid paymentId);
//     Task<ApiResponse<BillPaymentStatusResponse>> GetBillPaymentStatusAsync(Guid billId);
//     
//     // SePay Webhook handler
//     Task<ApiResponse<object>> HandleSePayWebhookAsync(string accountNumber, decimal amount, string content, string transactionId);
//     
//     // Payment history
//     Task<ApiResponse<List<PaymentHistoryResponse>>> GetPaymentHistoryAsync(Guid paymentId);
//     Task<ApiResponse<List<PaymentHistoryResponse>>> GetBillPaymentHistoryAsync(Guid billId);
// }
//
// // Response DTOs
// public class PaymentResponse
// {
//     public Guid Id { get; set; }
//     public Guid UtilityBillId { get; set; }
//     public Guid TenantId { get; set; }
//     public Guid? OwnerId { get; set; }
//     public decimal Amount { get; set; }
//     public PaymentMethod PaymentMethod { get; set; }
//     public PaymentStatus Status { get; set; }
//     public string? TransactionId { get; set; }
//     public string? TransactionContent { get; set; }
//     public string? Notes { get; set; }
//     public DateTime CreatedAt { get; set; }
//     public DateTime? PaidAt { get; set; }
// }
//
// public class PaymentQRInfoResponse
// {
//     public Guid BillId { get; set; }
//     public decimal Amount { get; set; }
//     public string QrDataUrl { get; set; } = string.Empty;
//     public string AccountNumber { get; set; } = string.Empty;
//     public string AccountName { get; set; } = string.Empty;
//     public string BankName { get; set; } = string.Empty;
//     public string PaymentContent { get; set; } = string.Empty;
//     public string PaymentCode { get; set; } = string.Empty;
// }
//
// public class PaymentStatusResponse
// {
//     public Guid PaymentId { get; set; }
//     public PaymentStatus Status { get; set; }
//     public bool IsPaid { get; set; }
//     public string? TransactionId { get; set; }
//     public DateTime? PaidAt { get; set; }
// }
//
// public class BillPaymentStatusResponse
// {
//     public Guid BillId { get; set; }
//     public bool IsPaid { get; set; }
//     public string Status { get; set; } = "Pending";
//     public Guid? PaymentId { get; set; }
//     public string? TransactionId { get; set; }
//     public decimal? PaidAmount { get; set; }
//     public DateTime? PaidDate { get; set; }
//     public string? Message { get; set; }
// }
//
// public class PaymentHistoryResponse
// {
//     public Guid Id { get; set; }
//     public Guid PaymentId { get; set; }
//     public Guid UtilityBillId { get; set; }
//     public string? SePayTransactionId { get; set; }
//     public decimal Amount { get; set; }
//     public string? TransactionContent { get; set; }
//     public string? AccountNumber { get; set; }
//     public string? Gateway { get; set; }
//     public PaymentStatus PreviousStatus { get; set; }
//     public PaymentStatus NewStatus { get; set; }
//     public string? StatusChangeReason { get; set; }
//     public DateTime TransactionDate { get; set; }
//     public DateTime CreatedAt { get; set; }
// }


using PaymentAPI.DTOs.Requests;
using PaymentAPI.Model;
using Shared.DTOs;
using Shared.Enums;

namespace PaymentAPI.Services.Interfaces;

public interface IPaymentService
{
    Task<ApiResponse<bool>> HandleSePayWebhookAsync(CreatePayment request);
    
  
    Task<ApiResponse<List<Payment>>> GetPaymentHistoryByTenantIdAsync(Guid userId);
    Task<ApiResponse<List<Payment>>> GetPaymentHistoryByOwnerIdAsync(Guid ownerId);

    /// <summary>
    /// Lấy lịch sử thanh toán theo BillId
    /// </summary>
    Task<ApiResponse<List<Payment>>> GetPaymentHistoryByBillIdAsync(Guid billId);
    
    /// <summary>
    /// Lấy chi tiết một payment theo ID
    /// </summary>
    Task<ApiResponse<Payment>> GetPaymentByIdAsync(Guid paymentId);
    
    /// <summary>
    /// Check trạng thái thanh toán của bill
    /// </summary>
    Task<ApiResponse<BillPaymentStatusResponse>> GetBillPaymentStatusAsync(Guid billId);
}

// Response DTO cho bill payment status
public class BillPaymentStatusResponse
{
    public Guid BillId { get; set; }
    public bool IsPaid { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid? PaymentId { get; set; }
    public string? TransactionId { get; set; }
    public decimal? PaidAmount { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? Message { get; set; }
}
