using Shared.DTOs.UtilityReadings.Responses;

namespace UtilityBillAPI.Service.Interface
{
    public interface IUtilityReadingService
    {
       Task<UtilityReadingResponse?> GetElectricityReadingAsync(Guid contractId, int month, int year);
       Task<UtilityReadingResponse?> GetWaterReadingAsync(Guid contractId, int month, int year);               
    }
}
