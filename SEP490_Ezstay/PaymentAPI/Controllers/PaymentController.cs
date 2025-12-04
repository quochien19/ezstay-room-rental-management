// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using PaymentAPI.DTOs.Requests;
// using PaymentAPI.Services.Interfaces;
// using System.Security.Claims;
//
// namespace PaymentAPI.Controllers;
//
// [ApiController]
// [Route("api/[controller]")]
// public class PaymentController : ControllerBase
// {
//     private readonly IPaymentService _paymentService;
//     private readonly ILogger<PaymentController> _logger;
//
//     public PaymentController(
//         IPaymentService paymentService,
//         ILogger<PaymentController> logger)
//     {
//         _paymentService = paymentService;
//         _logger = logger;
//     }
//
//     /// <summary>
//     /// Tạo payment mới (Online hoặc Offline)
//     /// ⚠️ DEPRECATED for Online - Use GetPaymentQRInfo instead
//     /// Chỉ dùng cho Offline payment
//     /// </summary>
//     [HttpPost("create")]
//     [Authorize]
//     public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
//     {
//         var tenantId = GetCurrentUserId();
//         var result = await _paymentService.CreatePaymentAsync(request, tenantId);
//         
//         if (!result.IsSuccess)
//         {
//             return BadRequest(result);
//         }
//         
//         return Ok(result);
//     }
//
//     /// <summary>
//     /// ⭐ NEW: Lấy thông tin QR để thanh toán KHÔNG TẠO PAYMENT
//     /// Payment chỉ được tạo khi webhook về (user đã chuyển khoản)
//     /// </summary>
//     [HttpGet("qr/{billId}")]
//     [Authorize]
//     public async Task<IActionResult> GetPaymentQRInfo(Guid billId)
//     {
//         var tenantId = GetCurrentUserId();
//         var result = await _paymentService.GetPaymentQRInfoAsync(billId, tenantId);
//         
//         if (!result.IsSuccess)
//         {
//             return BadRequest(result);
//         }
//         
//         return Ok(result);
//     }
//     
//     /// <summary>
//     /// Verify online payment (check với SePay)
//     /// </summary>
//     [HttpPost("verify-online")]
//     [Authorize]
//     public async Task<IActionResult> VerifyOnlinePayment([FromBody] VerifyOnlinePaymentRequest request)
//     {
//         var result = await _paymentService.VerifyOnlinePaymentAsync(request);
//         
//         if (!result.IsSuccess)
//         {
//             return BadRequest(result);
//         }
//         
//         return Ok(result);
//     }
//
//
//
//     /// <summary>
//     /// Lấy chi tiết 1 payment
//     /// </summary>
//     [HttpGet("{paymentId}")]
//     [Authorize]
//     public async Task<IActionResult> GetPaymentById(Guid paymentId)
//     {
//         var result = await _paymentService.GetPaymentByIdAsync(paymentId);
//         
//         if (!result.IsSuccess)
//         {
//             return NotFound(result);
//         }
//         
//         return Ok(result);
//     }
//
//     /// <summary>
//     /// Lấy danh sách payment của 1 bill
//     /// </summary>
//     [HttpGet("bill/{billId}")]
//     [Authorize]
//     public async Task<IActionResult> GetPaymentsByBillId(Guid billId)
//     {
//         var result = await _paymentService.GetPaymentsByBillIdAsync(billId);
//         
//         if (!result.IsSuccess)
//         {
//             return BadRequest(result);
//         }
//         
//         return Ok(result);
//     }
//
//     /// <summary>
//     /// Lấy danh sách payment của user (tenant)
//     /// </summary>
//     [HttpGet("my-payments")]
//     [Authorize]
//     public async Task<IActionResult> GetMyPayments()
//     {
//         var userId = GetCurrentUserId();
//         var result = await _paymentService.GetPaymentsByUserIdAsync(userId);
//         
//         if (!result.IsSuccess)
//         {
//             return BadRequest(result);
//         }
//         
//         return Ok(result);
//     }
//     
//     /// <summary>
//     /// Lấy payment mới nhất của 1 bill (dùng cho trang BillDetail)
//     /// </summary>
//     [HttpGet("bill/{billId}/latest")]
//     [Authorize]
//     public async Task<IActionResult> GetLatestPaymentByBillId(Guid billId)
//     {
//         var result = await _paymentService.GetLatestPaymentByBillIdAsync(billId);
//         
//         if (!result.IsSuccess)
//         {
//             return NotFound(result);
//         }
//         
//         return Ok(result);
//     }
//
//     /// <summary>
//     /// Check trạng thái payment (dùng cho polling sau khi scan QR)
//     /// </summary>
//     [HttpGet("{paymentId}/status")]
//     [Authorize]
//     public async Task<IActionResult> CheckPaymentStatus(Guid paymentId)
//     {
//         var result = await _paymentService.CheckPaymentStatusAsync(paymentId);
//         
//         if (!result.IsSuccess)
//         {
//             return NotFound(result);
//         }
//         
//         return Ok(result);
//     }
//
//     /// <summary>
//     /// Webhook endpoint để nhận thông báo từ SePay khi có giao dịch mới
//     /// URL: /api/Payment/webhook/sepay
//     /// </summary>
//     [HttpPost("hook/sepay")]
//     [AllowAnonymous] // Webhook không cần auth
//     public async Task<IActionResult> SePayWebhook([FromBody] SePayWebhookRequest request)
//     {
//         try
//         {
//             // TODO: Uncomment khi SePay config webhook secret
//             _logger.LogInformation($"🔔 Received SePay webhook: {System.Text.Json.JsonSerializer.Serialize(request)}");
//             _logger.LogInformation($"📝 Content: {request.Content}");
//             _logger.LogInformation($"💰 Amount: {request.TransferAmount}");
//             _logger.LogInformation($"🏦 Account: {request.AccountNumber}");
//             _logger.LogInformation($"🆔 Transaction ID: {request.Id}");
//             
//             // 3. Process webhook - Use Content field (not Description)
//             var result = await _paymentService.HandleSePayWebhookAsync(
//                 request.AccountNumber,
//                 request.TransferAmount,
//                 request.Content,  // Use Content instead of Description
//                 request.Id  // Use Id instead of TransactionId
//             );
//             
//             if (!result.IsSuccess)
//             {
//                 _logger.LogError($"Webhook processing failed: {result.Message}");
//                 return BadRequest(result);
//             }
//             
//             _logger.LogInformation($"Webhook processed successfully for transaction {request.TransactionId}");
//             return Ok(result);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error processing webhook");
//             return StatusCode(500, new { error = "Internal server error" });
//         }
//     }
//
//     /// <summary>
//     /// Check xem bill đã được thanh toán chưa (dùng cho polling từ frontend)
//     /// Frontend có thể gọi API này mỗi 3-5s sau khi show QR
//     /// </summary>
//     [HttpGet("bill/{billId}/payment-status")]
//     [Authorize]
//     public async Task<IActionResult> GetBillPaymentStatus(Guid billId)
//     {
//         var result = await _paymentService.GetBillPaymentStatusAsync(billId);
//         
//         if (!result.IsSuccess)
//         {
//             return BadRequest(result);
//         }
//         
//         return Ok(result);
//     }
//
//     /// <summary>
//     /// Check bill payment status - Polling endpoint để frontend tự động check
//     /// Chỉ check database, không gọi SePay API
//     /// </summary>
//     [HttpGet("check-payment/{billId}")]
//     [AllowAnonymous] // Allow polling without auth
//     public async Task<IActionResult> CheckBillPaymentStatus(Guid billId)
//     {
//         try
//         {
//             // Get bill payment status from database only (not calling SePay)
//             var result = await _paymentService.GetBillPaymentStatusAsync(billId);
//             
//             if (!result.IsSuccess)
//             {
//                 return Ok(new { isPaid = false, message = result.Message });
//             }
//             
//             // Check if IsPaid is true or Status is "Success"
//             var isPaid = result.Data?.IsPaid == true || result.Data?.Status == "Success";
//             
//             return Ok(new 
//             { 
//                 isPaid = isPaid,
//                 status = result.Data?.Status,
//                 paymentId = result.Data?.PaymentId,
//                 transactionId = result.Data?.TransactionId,
//                 paidAmount = result.Data?.PaidAmount,
//                 paidDate = result.Data?.PaidDate,
//                 message = result.Data?.Message ?? result.Message 
//             });
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error checking payment status for bill {BillId}", billId);
//             return Ok(new { isPaid = false, message = "Chưa thanh toán" });
//         }
//     }
//
//     /// <summary>
//     /// Lấy lịch sử thanh toán của một payment
//     /// </summary>
//     [HttpGet("{paymentId}/history")]
//     [Authorize]
//     public async Task<IActionResult> GetPaymentHistory(Guid paymentId)
//     {
//         var result = await _paymentService.GetPaymentHistoryAsync(paymentId);
//         
//         if (!result.IsSuccess)
//         {
//             return BadRequest(result);
//         }
//         
//         return Ok(result);
//     }
//
//     /// <summary>
//     /// Lấy lịch sử thanh toán của một bill
//     /// </summary>
//     [HttpGet("bill/{billId}/history")]
//     [Authorize]
//     public async Task<IActionResult> GetBillPaymentHistory(Guid billId)
//     {
//         var result = await _paymentService.GetBillPaymentHistoryAsync(billId);
//         
//         if (!result.IsSuccess)
//         {
//             return BadRequest(result);
//         }
//         
//         return Ok(result);
//     }
//
//     // ========================================
//     // FLOW 2: MANUAL CHECK - COMMENTED OUT
//     // Uncomment khi cần backup cho webhook
//     // ========================================
//     /*
//     /// <summary>
//     /// Manual check payment - User nhấn "Đã chuyển khoản, kiểm tra ngay"
//     /// API này sẽ chủ động gọi SePay để tìm giao dịch
//     /// </summary>
//     [HttpPost("check-payment-manual/{billId}")]
//     [AllowAnonymous] // Allow without auth for testing
//     public async Task<IActionResult> CheckPaymentManual(Guid billId)
//     {
//         _logger.LogInformation($"🔍 Manual payment check requested for bill: {billId}");
//         
//         // Get userId if authenticated, otherwise use empty Guid
//         Guid userId = Guid.Empty;
//         try
//         {
//             userId = GetCurrentUserId();
//         }
//         catch
//         {
//             // User not authenticated, use empty Guid
//             _logger.LogInformation($"ℹ️ Anonymous manual check for bill: {billId}");
//         }
//         
//         var result = await _paymentService.CheckPaymentManualAsync(billId, userId);
//         
//         if (!result.IsSuccess)
//         {
//             return BadRequest(result);
//         }
//         
//         return Ok(result);
//     }
//     */
//
//     // Helper method
//     private Guid GetCurrentUserId()
//     {
//         var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
//                          ?? User.FindFirst("userId")?.Value
//                          ?? User.FindFirst("sub")?.Value;
//         
//         if (string.IsNullOrEmpty(userIdClaim))
//         {
//             throw new UnauthorizedAccessException("User not authenticated");
//         }
//
//         return Guid.Parse(userIdClaim);
//     }
// }
//
// // DTO for SePay Webhook - Match với format từ SePay
// public class SePayWebhookRequest
// {
//     // SePay fields
//     public string Gateway { get; set; } = string.Empty;
//     public string AccountNumber { get; set; } = string.Empty;
//     // public string? SubAccount { get; set; }
//     // public string? Code { get; set; }
//     public string Content { get; set; } = string.Empty;
//     // public string TransferType { get; set; } = string.Empty;
//     // public string? Description { get; set; }
//     public decimal TransferAmount { get; set; }
//     // public string? ReferenceCode { get; set; }
//     // public decimal Accumulated { get; set; }
//     
//     // Transaction ID - SePay gửi dạng số (int/long)
//     [System.Text.Json.Serialization.JsonPropertyName("id")]
//     public long IdNumber { get; set; }
//     
//     // Convert to string for use
//     [System.Text.Json.Serialization.JsonIgnore]
//     public string Id => IdNumber.ToString();
//     
//     // Legacy compatibility - map từ fields mới
//     [System.Text.Json.Serialization.JsonIgnore]
//     public decimal Amount => TransferAmount;
//     
//     [System.Text.Json.Serialization.JsonIgnore]
//     public string TransactionId => Id;
//     
//     [System.Text.Json.Serialization.JsonIgnore]
//     public string BankBrandName => Gateway;
// }


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentAPI.DTOs.Requests;
using PaymentAPI.Services.Interfaces;
using System.Security.Claims;

namespace PaymentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
 
    public PaymentController(
        IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }
    
    [HttpPost("hook/sepay")]
    [AllowAnonymous]
    public async Task<IActionResult> SePayWebhook([FromBody] CreatePayment request)
    {
          var result = await _paymentService.HandleSePayWebhookAsync(request);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
    }
    
    [HttpGet("history/user")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> GetPaymentHistoryByUser()
    {
        var tenantId = GetCurrentUserId();
        var result = await _paymentService.GetPaymentHistoryByTenantIdAsync(tenantId);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.Message);
        }
        
        return Ok(result);
    }
    [HttpGet("history/owner")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetPaymentHistoryByOwner()
    {
        var userId = GetCurrentUserId();
        var result = await _paymentService.GetPaymentHistoryByOwnerIdAsync(userId);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.Message);
        }
        return Ok(result);
    }
    [HttpGet("bill/{billId}/history")]
    [Authorize]
    public async Task<IActionResult> GetBillPaymentHistory(Guid billId)
    {
        var result = await _paymentService.GetPaymentHistoryByBillIdAsync(billId);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        
        return Ok(result);
    }
    // /// <summary>
    // /// Lấy lịch sử thanh toán của một bill cụ thể
    // /// </summary>
    // [HttpGet("bill/{billId}/history")]
    // [Authorize]
    // public async Task<IActionResult> GetBillPaymentHistory(Guid billId)
    // {
    //     var result = await _paymentService.GetPaymentHistoryByBillIdAsync(billId);
    //     
    //     if (!result.IsSuccess)
    //     {
    //         return BadRequest(result);
    //     }
    //     
    //     return Ok(result);
    // }

    /// <summary>
    /// Lấy chi tiết một payment theo ID
    /// </summary>
    [HttpGet("{paymentId}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentById(Guid paymentId)
    {
        var result = await _paymentService.GetPaymentByIdAsync(paymentId);
        
        if (!result.IsSuccess)
        {
            return NotFound(result);
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Check trạng thái thanh toán của bill (dùng cho polling)
    /// </summary>
    [HttpGet("bill/{billId}/payment-status")]
    [Authorize]
    public async Task<IActionResult> GetBillPaymentStatus(Guid billId)
    {
        var result = await _paymentService.GetBillPaymentStatusAsync(billId);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        
        return Ok(result);
    }
    
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? User.FindFirst("userId")?.Value
                         ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        return Guid.Parse(userIdClaim);
    }
}
