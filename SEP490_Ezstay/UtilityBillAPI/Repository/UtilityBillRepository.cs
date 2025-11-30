using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Shared.Enums;
using UtilityBillAPI.Data;
using UtilityBillAPI.Models;
using UtilityBillAPI.Repository.Interface;

namespace UtilityBillAPI.Repository
{
    public class UtilityBillRepository : IUtilityBillRepository
    {
        private readonly IMongoCollection<UtilityBill> _utilityBills;
        private readonly IMongoCollection<UtilityBillDetail> _utilityBillDetails;   

        public UtilityBillRepository(MongoDbService service)
        {
            _utilityBills = service.UtilityBills;
            _utilityBillDetails = service.UtilityBillDetails;
        }

        public IQueryable<UtilityBill> GetAll()
        {
            return _utilityBills.AsQueryable();
        }             

        public IQueryable<UtilityBill> GetAllByTenant(Guid tenantId)
        {
            return _utilityBills.AsQueryable().Where(b => b.TenantId == tenantId);
        }

        public IQueryable<UtilityBill> GetAllByOwner(Guid ownerId)
        {
            return _utilityBills.AsQueryable().Where(b => b.OwnerId == ownerId);
        }
        
        public IQueryable<UtilityBill> GetAllByContract(Guid contractId)
        {
            return _utilityBills.AsQueryable().Where(b => b.ContractId == contractId);
        }

        public async Task<UtilityBill?> GetByIdAsync(Guid id)
        {
            return await _utilityBills.Find(b => b.Id == id).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(UtilityBill bill)
        {  
            if (bill.Details != null && bill.Details.Count > 0)
            {
                foreach (var detail in bill.Details)
                {
                    detail.UtilityBillId = bill.Id;                    
                }
                await _utilityBillDetails.InsertManyAsync(bill.Details);
            }
            await _utilityBills.InsertOneAsync(bill);
        }

        public async Task MarkAsPaidAsync(Guid billId)
        {
            var update = Builders<UtilityBill>.Update
               .Set(b => b.Status, UtilityBillStatus.Paid)               
               .Set(b => b.UpdatedAt, DateTime.UtcNow);

            await _utilityBills.UpdateOneAsync(b => b.Id == billId, update);
        }

        public async Task CancelAsync(Guid billId, string? reason)
        {
            var update = Builders<UtilityBill>.Update
                .Set(b => b.Status, UtilityBillStatus.Cancelled)
                .Set(b => b.UpdatedAt, DateTime.UtcNow);

            if (reason != null)
            {
                update = update.Set(b => b.Reason, reason);
            }

            await _utilityBills.UpdateOneAsync(b => b.Id == billId, update);
        }     
    }
}
