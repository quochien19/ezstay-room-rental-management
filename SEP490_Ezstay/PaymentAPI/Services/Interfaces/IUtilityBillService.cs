using Shared.DTOs.UtilityBills.Responses;

namespace PaymentAPI.Services.Interfaces;

public interface IUtilityBillService
{
    Task<bool> MarkBillAsPaidInternalAsync(Guid billId);
    
    /// <summary>
    /// Lấy thông tin Bill để lấy TenantId và OwnerId
    /// </summary>
    Task<UtilityBillResponse?> GetBillByIdAsync(Guid billId);
}
