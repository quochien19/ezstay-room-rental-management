using MongoDB.Driver;
using PaymentAPI.Model;
using PaymentAPI.Repository.Interface;

namespace PaymentAPI.Repository;

public class PaymentHistoryRepository : IPaymentHistoryRepository
{
    private readonly IMongoCollection<PaymentHistory> _histories;

    public PaymentHistoryRepository(IMongoDatabase database)
    {
        _histories = database.GetCollection<PaymentHistory>("PaymentHistories");
        
        // Create indexes
        var indexKeys = Builders<PaymentHistory>.IndexKeys;
        _histories.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<PaymentHistory>(indexKeys.Ascending(h => h.PaymentId)),
            new CreateIndexModel<PaymentHistory>(indexKeys.Ascending(h => h.UtilityBillId)),
            new CreateIndexModel<PaymentHistory>(indexKeys.Ascending(h => h.SePayTransactionId))
        });
    }

    public async Task<PaymentHistory?> GetByIdAsync(Guid id)
    {
        return await _histories.Find(h => h.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<PaymentHistory>> GetByPaymentIdAsync(Guid paymentId)
    {
        return await _histories.Find(h => h.PaymentId == paymentId)
            .SortByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PaymentHistory>> GetByBillIdAsync(Guid billId)
    {
        return await _histories.Find(h => h.UtilityBillId == billId)
            .SortByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<PaymentHistory?> GetBySePayTransactionIdAsync(string transactionId)
    {
        return await _histories.Find(h => h.SePayTransactionId == transactionId)
            .FirstOrDefaultAsync();
    }

    public async Task<PaymentHistory> CreateAsync(PaymentHistory history)
    {
        history.CreatedAt = DateTime.UtcNow;
        await _histories.InsertOneAsync(history);
        return history;
    }

    public async Task<bool> ExistsByTransactionIdAsync(string transactionId)
    {
        return await _histories.Find(h => h.SePayTransactionId == transactionId).AnyAsync();
    }
}
