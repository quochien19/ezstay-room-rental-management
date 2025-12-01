using PaymentAPI.Services.Interfaces;
using System.Text.Json;
using Shared.DTOs.UtilityBills.Responses;

namespace PaymentAPI.Services;

public class UtilityBillService : IUtilityBillService
{
   private readonly HttpClient _httpClient;
    private readonly ILogger<UtilityBillService> _logger;
    private readonly IConfiguration _configuration;
    public UtilityBillService(HttpClient httpClient, 
                             ILogger<UtilityBillService> logger,
                             IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        
        // Cấu hình BaseAddress từ AppSettings
        var baseUrl = _configuration.GetValue<string>("UtilityBillApiBaseUrl");
        if (string.IsNullOrEmpty(baseUrl))
        {
            _logger.LogError("UtilityBillApiBaseUrl is missing in configuration.");
            throw new InvalidOperationException("UtilityBillApiBaseUrl is not configured.");
        }
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<bool> MarkBillAsPaidInternalAsync(Guid billId)
    {
        if (billId == Guid.Empty)
        {
            _logger.LogWarning("MarkBillAsPaidInternalAsync called with empty BillId.");
            return false;
        }

        var internalApiUrl = $"/api/UtilityBills/{billId}/mark-paid-internal";

        try
        {
            // Endpoint MarkAsPaidInternal không yêu cầu Body, chỉ cần gọi PUT
            var response = await _httpClient.PutAsync(internalApiUrl, null);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"✅ Bill {billId} marked as paid successfully.");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"⚠️ Failed to mark bill {billId} as paid. Status: {response.StatusCode}. Error: {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calling UtilityBillAPI at {internalApiUrl} for bill {billId}");
            return false;
        }
    }
}