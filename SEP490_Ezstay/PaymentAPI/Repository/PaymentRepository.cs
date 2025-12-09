// using MongoDB.Driver;
// using PaymentAPI.Model;
// using PaymentAPI.Repository.Interface;
// using Shared.Enums;
//
// namespace PaymentAPI.Repository;
//
// public class PaymentRepository : IPaymentRepository
// {
//     private readonly IMongoCollection<Payment> _payments;
//
//     public PaymentRepository(IMongoDatabase database)
//     {
//         _payments = database.GetCollection<Payment>("Payments");
//         
//         // Create indexes
//         var indexKeys = Builders<Payment>.IndexKeys;
//         _payments.Indexes.CreateMany(new[]
//         {
//             new CreateIndexModel<Payment>(indexKeys.Ascending(p => p.UtilityBillId)),
//             new CreateIndexModel<Payment>(indexKeys.Ascending(p => p.TenantId)),
//             new CreateIndexModel<Payment>(indexKeys.Ascending(p => p.PaymentCode)),
//             new CreateIndexModel<Payment>(indexKeys.Ascending(p => p.Status))
//         });
//     }
//
//     public async Task<Payment?> GetByIdAsync(Guid id)
//     {
//         return await _payments.Find(p => p.Id == id).FirstOrDefaultAsync();
//     }
//
//     public async Task<Payment?> GetByBillIdAndStatusAsync(Guid billId, params PaymentStatus[] statuses)
//     {
//         var filter = Builders<Payment>.Filter.And(
//             Builders<Payment>.Filter.Eq(p => p.UtilityBillId, billId),
//             Builders<Payment>.Filter.In(p => p.Status, statuses)
//         );
//         return await _payments.Find(filter)
//             .SortByDescending(p => p.CreatedAt)
//             .FirstOrDefaultAsync();
//     }
//
//     public async Task<Payment?> GetByPaymentCodeAsync(string paymentCode)
//     {
//         return await _payments.Find(p => p.PaymentCode == paymentCode)
//             .SortByDescending(p => p.CreatedAt)
//             .FirstOrDefaultAsync();
//     }
//
//     public async Task<List<Payment>> GetByBillIdAsync(Guid billId)
//     {
//         return await _payments.Find(p => p.UtilityBillId == billId)
//             .SortByDescending(p => p.CreatedAt)
//             .ToListAsync();
//     }
//
//     public async Task<List<Payment>> GetByUserIdAsync(Guid userId)
//     {
//         return await _payments.Find(p => p.TenantId == userId)
//             .SortByDescending(p => p.CreatedAt)
//             .ToListAsync();
//     }
//
//     public async Task<List<Payment>> GetPendingApprovalsByOwnerIdAsync(Guid ownerId)
//     {
//         return await _payments.Find(p => 
//             p.OwnerId == ownerId && 
//             p.Status == PaymentStatus.PendingApproval)
//             .SortByDescending(p => p.CreatedAt)
//             .ToListAsync();
//     }
//
//     public async Task<Payment?> GetLatestByBillIdAsync(Guid billId)
//     {
//         return await _payments.Find(p => p.UtilityBillId == billId)
//             .SortByDescending(p => p.CreatedAt)
//             .FirstOrDefaultAsync();
//     }
//
//     public async Task<Payment> CreateAsync(Payment payment)
//     {
//         payment.CreatedAt = DateTime.UtcNow;
//         await _payments.InsertOneAsync(payment);
//         return payment;
//     }
//
//     public async Task<Payment> UpdateAsync(Payment payment)
//     {
//         payment.UpdatedAt = DateTime.UtcNow;
//         await _payments.ReplaceOneAsync(p => p.Id == payment.Id, payment);
//         return payment;
//     }
//
//     public async Task<bool> ExistsAsync(Guid id)
//     {
//         return await _payments.Find(p => p.Id == id).AnyAsync();
//     }
// }


using MongoDB.Driver;
using PaymentAPI.Model;
using PaymentAPI.Repository.Interface;

namespace PaymentAPI.Repository;

public class PaymentRepository : IPaymentRepository
{
    private readonly IMongoCollection<Payment> _payment;

    public PaymentRepository(IMongoDatabase database)
    {
        _payment = database.GetCollection<Payment>("Payments");
        
        // Create indexes for better query performance
        var indexKeys = Builders<Payment>.IndexKeys;
        _payment.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<Payment>(indexKeys.Ascending(p => p.TenantId)),
            new CreateIndexModel<Payment>(indexKeys.Ascending(p => p.OwnerId)),
         //   new CreateIndexModel<Payment>(indexKeys.Ascending(p => p.BillId)),
            new CreateIndexModel<Payment>(indexKeys.Ascending(p => p.TransactionId))
        });
    }

    public async Task<Payment> GetByIdAsync(Guid id)
    {
        return await _payment.Find(h => h.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Payment> GetBySePayTransactionIdAsync(string transactionId)
    {
        return await _payment.Find(h => h.TransactionId == transactionId)
            .FirstOrDefaultAsync();
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        await _payment.InsertOneAsync(payment);
        return payment;
    }

    public async Task<bool> ExistsByTransactionIdAsync(string transactionId)
    {
        return await _payment.Find(h => h.TransactionId == transactionId).AnyAsync();
    }
    
    public async Task<List<Payment>> GetPaymentsByTenantId(Guid tenantId)
    {
        return await _payment.Find(p => p.TenantId == tenantId)
            .SortByDescending(p => p.TransactionDate)
            .ToListAsync();
    }
    
    public async Task<List<Payment>> GetPaymentsByOwnerId(Guid ownerId)
    {
        return await _payment.Find(p => p.OwnerId == ownerId)
            .SortByDescending(p => p.TransactionDate)
            .ToListAsync();
    }
    
    public async Task<List<Payment>> GetByBillIdAsync(Guid billId)
    {
        return await _payment.Find(p => p.BillId == billId)
            .SortByDescending(p => p.TransactionDate)
            .ToListAsync();
    }
    
    public async Task<List<Payment>> GetAllPaymentsAsync()
    {
        return await _payment.Find(_ => true)
            .SortByDescending(p => p.TransactionDate)
            .ToListAsync();
    }
}
