using Shared.DTOs.UtilityReadings.Responses;
using Shared.Enums;
using UtilityBillAPI.Service.Interface;

namespace UtilityBillAPI.Service
{
    public class UtilityReadingService : IUtilityReadingService
    {
        private readonly HttpClient _httpClient;

        public UtilityReadingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private async Task<UtilityReadingResponse?> GetLatestReadingAsync(Guid contractId, UtilityType type, int month, int year)
        {
            var requestUrl = $"api/UtilityReading/latest/{contractId}/{type}/month/{month}/year/{year}";
            var response = await _httpClient.GetAsync(requestUrl);

            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<UtilityReadingResponse>()
                : null;
        }

        public Task<UtilityReadingResponse?> GetElectricityReadingAsync(Guid contractId, int month, int year) =>
            GetLatestReadingAsync(contractId, UtilityType.Electric, month, year);

        public Task<UtilityReadingResponse?> GetWaterReadingAsync(Guid contractId, int month, int year) =>
            GetLatestReadingAsync(contractId, UtilityType.Water, month, year);
       
    }

}
