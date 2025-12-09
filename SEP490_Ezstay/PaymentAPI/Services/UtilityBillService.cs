

using PaymentAPI.Services.Interfaces;
using Shared.DTOs.UtilityBills.Responses;
using System.Text.Json;

namespace PaymentAPI.Services;

/// <summary>
/// DTO để deserialize từ UtilityBillAPI response
/// </summary>
public class UtilityBillDto
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ContractId { get; set; }
    public string HouseName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public decimal? RoomPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
}

public class UtilityBillService : IUtilityBillService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UtilityBillService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UtilityBillService(HttpClient httpClient, 
        ILogger<UtilityBillService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> MarkBillAsPaidInternalAsync(Guid billId)
    {
        var endpoint = $"api/UtilityBills/{billId}/mark-paid-internal";
        var fullUrl = $"{_httpClient.BaseAddress}{endpoint}"; 
        _logger.LogInformation($"🚀 Starting call to UtilityBillAPI: {fullUrl}");

        try
        {
            var response = await _httpClient.PutAsync(endpoint, null);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"✅ Update Bill Success for ID: {billId}");
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"❌ Call failed. Status: {response.StatusCode}. Reason: {errorContent}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"🔥 CRASH/NETWORK ERROR when calling {fullUrl}");
            return false;
        }
    }
    
    public async Task<UtilityBillResponse?> GetBillByIdAsync(Guid billId)
    {
        var endpoint = $"api/UtilityBills/{billId}";
        var fullUrl = $"{_httpClient.BaseAddress}{endpoint}";
        _logger.LogInformation($"🔍 Getting bill info from: {fullUrl}");
        
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"📄 Raw response: {content}");
                
                // Deserialize to local DTO first
                var billDto = JsonSerializer.Deserialize<UtilityBillDto>(content, _jsonOptions);
                
                if (billDto != null)
                {
                    // Map to UtilityBillResponse
                    var result = new UtilityBillResponse
                    {
                        Id = billDto.Id,
                        OwnerId = billDto.OwnerId,
                        TenantId = billDto.TenantId,
                        ContractId = billDto.ContractId,
                        RoomPrice = billDto.RoomPrice ?? 0,
                        TotalAmount = billDto.TotalAmount
                    };
                    
                    _logger.LogInformation($"✅ Got bill info for ID: {billId}, TenantId: {result.TenantId}, OwnerId: {result.OwnerId}");
                    return result;
                }
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning($"⚠️ Cannot get bill {billId}. Status: {response.StatusCode}. Error: {errorContent}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"🔥 Error getting bill info from {fullUrl}");
            return null;
        }
    }
}