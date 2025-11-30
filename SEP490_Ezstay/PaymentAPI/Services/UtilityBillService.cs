using PaymentAPI.Services.Interfaces;
using System.Text.Json;
using Shared.DTOs.UtilityBills.Responses;

namespace PaymentAPI.Services;

public class UtilityBillService : IUtilityBillService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UtilityBillService> _logger;
    private readonly IConfiguration _configuration;

    public UtilityBillService(
        HttpClient httpClient,
        ILogger<UtilityBillService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        // BaseAddress is set in Program.cs via HttpClient factory
    }

    public async Task<UtilityBillResponse?> GetBillByIdAsync(Guid billId)
    {
        try
        {
            _logger.LogInformation($"🔍 Getting bill {billId} from UtilityBillAPI...");
            _logger.LogInformation($"🌐 HttpClient BaseAddress: {_httpClient.BaseAddress}");
            
            var endpoint = $"api/UtilityBills/{billId}";
            var fullUrl = _httpClient.BaseAddress != null 
                ? new Uri(_httpClient.BaseAddress, endpoint).ToString()
                : endpoint;
            _logger.LogInformation($"📍 Full URL: {fullUrl}");
            
            var response = await _httpClient.GetAsync(endpoint);
            _logger.LogInformation($"📥 Response status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"❌ Failed to get bill {billId}. Status: {response.StatusCode}, Error: {errorContent}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"📄 Response content length: {content.Length} characters");
            
            var bill = JsonSerializer.Deserialize<UtilityBillResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (bill != null)
            {
                _logger.LogInformation($"✅ Bill found: ID={bill.Id}, Amount={bill.TotalAmount}, Status={bill.Status}");
            }
            else
            {
                _logger.LogWarning($"⚠️ Bill deserialized to null");
            }

            return bill;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Exception getting bill {billId}: {ex.Message}");
            _logger.LogError($"Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    public async Task<bool> UpdateBillStatusAsync(Guid billId, string status, DateTime? paymentDate)
    {
        try
        {
            _logger.LogInformation($"📞 Calling UtilityBillAPI to mark bill {billId} as paid...");
            _logger.LogInformation($"🌐 HttpClient BaseAddress: {_httpClient.BaseAddress}");
            
            // Call internal endpoint (no auth required for service-to-service calls)
            // Endpoint: PUT /api/UtilityBills/{billId}/mark-paid-internal
            var endpoint = $"api/UtilityBills/{billId}/mark-paid-internal";
            var fullUrl = _httpClient.BaseAddress != null 
                ? new Uri(_httpClient.BaseAddress, endpoint).ToString()
                : endpoint;
            
            _logger.LogInformation($"📍 Full URL: {fullUrl}");
            
            var response = await _httpClient.PutAsync(endpoint, null);
            
            _logger.LogInformation($"📥 Response status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"❌ Failed to mark bill as paid. Status: {response.StatusCode}, Error: {errorContent}");
                return false;
            }

            var successContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"✅ Successfully marked bill {billId} as paid. Response: {successContent}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error updating bill {billId} status: {ex.Message}");
            _logger.LogError($"❌ Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    // public async Task<List<UtilityBillResponse>> GetBillsByOwnerIdAsync(Guid ownerId)
    // {
    //     try
    //     {
    //         var response = await _httpClient.GetAsync($"/api/UtilityBill/owner/{ownerId}");
    //         
    //         if (!response.IsSuccessStatusCode)
    //         {
    //             _logger.LogError($"Failed to get bills for owner {ownerId}");
    //             return new List<UtilityBillResponse>();
    //         }
    //
    //         var content = await response.Content.ReadAsStringAsync();
    //         var bills = JsonSerializer.Deserialize<List<dynamic>>(content, new JsonSerializerOptions
    //         {
    //             PropertyNameCaseInsensitive = true
    //         });
    //
    //         return bills ?? new List<UtilityBillResponse>();
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, $"Error getting bills for owner {ownerId}");
    //         return new List<UtilityBillResponse>();
    //     }
    //}
}
