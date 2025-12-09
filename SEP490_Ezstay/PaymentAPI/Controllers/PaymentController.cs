
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
    
    // [HttpGet("stats/owner")]
    // [Authorize(Roles = "Owner")]
    // public async Task<IActionResult> GetOwnerStats([FromQuery] int? year)
    // {
    //     var ownerId = GetCurrentUserId();
    //     var result = await _paymentService.GetOwnerRevenueStatsAsync(ownerId, year);
    //     return Ok(result);
    // }


    
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
