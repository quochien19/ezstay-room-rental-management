using PaymentAPI.Services.Interfaces;
using System.Text.Json;
using Shared.DTOs.UtilityBills.Responses;

namespace PaymentAPI.Services;

public class UtilityBillService : IUtilityBillService
{
   private readonly HttpClient _httpClient;
    private readonly ILogger<UtilityBillService> _logger;
    // private readonly IConfiguration _configuration;
    public UtilityBillService(HttpClient httpClient, 
                             ILogger<UtilityBillService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> MarkBillAsPaidInternalAsync(Guid billId)
    {
        // if (billId == Guid.Empty)
        // {
        //     _logger.LogWarning("MarkBillAsPaidInternalAsync called with empty BillId.");
        //     return false;
        // }

        var internalApiUrl = $"api/UtilityBills/{billId}/mark-paid-internal";
            // Endpoint MarkAsPaidInternal không yêu cầu Body, chỉ cần gọi PUT
            var response = await _httpClient.PutAsync(internalApiUrl, null);

           return response.IsSuccessStatusCode;

            // }
            // catch (Exception ex)
            // {
            //     _logger.LogError(ex, $"Error calling UtilityBillAPI at {internalApiUrl} for bill {billId}");
            //     return false;
            // }
    }
}