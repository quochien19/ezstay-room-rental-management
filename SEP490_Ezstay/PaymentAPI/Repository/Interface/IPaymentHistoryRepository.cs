using PaymentAPI.Model;

namespace PaymentAPI.Repository.Interface;

public interface IPaymentHistoryRepository
{
    Task<PaymentHistory?> GetByIdAsync(Guid id);
    Task<List<PaymentHistory>> GetByPaymentIdAsync(Guid paymentId);

    Task<List<PaymentHistory>> GetByBillIdAsync(Guid billId);
    Task<PaymentHistory?> GetBySePayTransactionIdAsync(string transactionId);

    Task<PaymentHistory> CreateAsync(PaymentHistory history);

    Task<bool> ExistsByTransactionIdAsync(string transactionId);


}