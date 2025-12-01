using Shared.DTOs.UtilityBills.Responses;

namespace PaymentAPI.Services.Interfaces;

public interface IUtilityBillService
{
    Task<bool> MarkBillAsPaidInternalAsync(Guid billId);
   
}
