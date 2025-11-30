using UtilityBillAPI.Models;

namespace UtilityBillAPI.Repository.Interface
{
    public interface IUtilityBillRepository
    {
        IQueryable<UtilityBill> GetAll();
        IQueryable<UtilityBill> GetAllByTenant(Guid tenantId);
        IQueryable<UtilityBill> GetAllByOwner(Guid ownerId);
        IQueryable<UtilityBill> GetAllByContract(Guid contractId); 
        Task<UtilityBill?> GetByIdAsync(Guid id);               
        Task CreateAsync(UtilityBill bill);                   
        Task MarkAsPaidAsync(Guid billId);
        Task CancelAsync(Guid billId, string? reason);                
    }
}
