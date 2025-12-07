// using PaymentAPI.Model;
// using Shared.Enums;
//
// namespace PaymentAPI.Repository.Interface;
//
// public interface IPaymentRepository
// {
//  
//  Task<Payment?> GetByIdAsync(Guid id);
//
//    Task<Payment?> GetByBillIdAndStatusAsync(Guid billId, params PaymentStatus[] statuses);
//
//     Task<Payment?> GetByPaymentCodeAsync(string paymentCode);
//
//      Task<List<Payment>> GetByBillIdAsync(Guid billId);
//
//      Task<List<Payment>> GetByUserIdAsync(Guid userId);
//      Task<List<Payment>> GetPendingApprovalsByOwnerIdAsync(Guid ownerId);
//
//     Task<Payment?> GetLatestByBillIdAsync(Guid billId);
//
//    Task<Payment> CreateAsync(Payment payment);
//
//     Task<Payment> UpdateAsync(Payment payment);
//
//     Task<bool> ExistsAsync(Guid id);
// }


using PaymentAPI.Model;

namespace PaymentAPI.Repository.Interface;

public interface IPaymentRepository
{
    Task<Payment> GetByIdAsync(Guid id);
    // Task<Payment> GetBySePayTransactionIdAsync(string transactionId);
    Task<Payment> CreateAsync(Payment payment);
    // Task<bool> ExistsByTransactionIdAsync(string transactionId);
    
    /// <summary>
    /// Lấy lịch sử thanh toán theo TenantId (người thuê)
    /// </summary>
    Task<List<Payment>> GetPaymentsByTenantId(Guid tenantId);
    
    /// <summary>
    /// Lấy lịch sử thanh toán theo OwnerId (chủ trọ)
    /// </summary>
    Task<List<Payment>> GetPaymentsByOwnerId(Guid ownerId);
    
    /// <summary>
    /// Lấy lịch sử thanh toán theo BillId
    /// </summary>
    Task<List<Payment>> GetByBillIdAsync(Guid billId);

    // Task<List<Payment>> GetAllPaymentsAsync();



}
