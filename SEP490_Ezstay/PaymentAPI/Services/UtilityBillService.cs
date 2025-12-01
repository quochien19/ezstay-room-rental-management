// using PaymentAPI.Services.Interfaces;
// using System.Text.Json;
// using Shared.DTOs.UtilityBills.Responses;
//
// namespace PaymentAPI.Services;
//
// public class UtilityBillService : IUtilityBillService
// {
//    private readonly HttpClient _httpClient;
//     private readonly ILogger<UtilityBillService> _logger;
//     // private readonly IConfiguration _configuration;
//     
//     public UtilityBillService(HttpClient httpClient, 
//                              ILogger<UtilityBillService> logger)
//     {
//         _httpClient = httpClient;
//         _logger = logger;
//         _httpClient = httpClient;
//     }
//
//     public async Task<bool> MarkBillAsPaidInternalAsync(Guid billId)
//     {
//         // if (billId == Guid.Empty)
//         // {
//         //     _logger.LogWarning("MarkBillAsPaidInternalAsync called with empty BillId.");
//         //     return false;
//         // }
//
//         var internalApiUrl = $"api/UtilityBills/{billId}/mark-paid-internal";
//             // Endpoint MarkAsPaidInternal không yêu cầu Body, chỉ cần gọi PUT
//             var response = await _httpClient.PutAsync(internalApiUrl, null);
//
//            return response.IsSuccessStatusCode;
//
//             // }
//             // catch (Exception ex)
//             // {
//        //     _logger.LogError(ex, $"Error calling UtilityBillAPI at {internalApiUrl} for bill {billId}");
//             //     return false;
//             // }
//     }
// }

using PaymentAPI.Services.Interfaces;

namespace PaymentAPI.Services;

public class UtilityBillService : IUtilityBillService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UtilityBillService> _logger;

    public UtilityBillService(HttpClient httpClient, 
        ILogger<UtilityBillService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> MarkBillAsPaidInternalAsync(Guid billId)
    {
        // 1. Log ra URL sắp gọi để kiểm tra xem BaseUrl đã nhận chưa
        var endpoint = $"api/UtilityBills/{billId}/mark-paid-internal";
        var fullUrl = $"{_httpClient.BaseAddress}{endpoint}"; 
        _logger.LogInformation($"🚀 Starting call to UtilityBillAPI: {fullUrl}");

        try
        {
            var response = await _httpClient.PutAsync(endpoint, null);

            // 2. Nếu THÀNH CÔNG (200-299)
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"✅ Update Bill Success for ID: {billId}");
                return true;
            }

            // 3. Nếu THẤT BẠI (400, 404, 500...) -> ĐỌC NỘI DUNG LỖI
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"❌ Call failed. Status: {response.StatusCode}. Reason: {errorContent}");
            return false;
        }
        catch (Exception ex)
        {
            // 4. Nếu SẬP MẠNG (DNS Error, Connection Refused...)
            _logger.LogError(ex, $"🔥 CRASH/NETWORK ERROR when calling {fullUrl}");
            return false;
        }
    }
}