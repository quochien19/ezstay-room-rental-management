

using AutoMapper;
using PaymentAPI.DTOs.Requests;
using PaymentAPI.DTOs.Responses;
using PaymentAPI.Model;
using PaymentAPI.Repository.Interface;
using PaymentAPI.Services.Interfaces;
using Shared.DTOs;
using Shared.Enums;
using System.Text.Json;

namespace PaymentAPI.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUtilityBillService _utilityBillService;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IUtilityBillService utilityBillService,
        IMapper mapper,
        ILogger<PaymentService> logger) {
        _paymentRepository = paymentRepository;
        _utilityBillService = utilityBillService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> HandleSePayWebhookAsync(CreatePayment request)
    {
        var billId = ExtractBillIdFromContent(request.Content);
        var bill = await _utilityBillService.GetBillByIdAsync(billId);
            
        var payment = new Payment
        {
            BillId = billId,
            TenantId =  bill.TenantId,
            OwnerId =  bill.OwnerId,
            TransactionId = request.TransactionId,
            TransferAmount = request.TransferAmount,
            Content = request.Content,
            AccountNumber = request.AccountNumber,
            Gateway = request.Gateway,
            TransferType = request.TransferType,
            TransactionDate = DateTime.UtcNow,
        };
      
        await _paymentRepository.CreateAsync(payment);
     
        if (billId != Guid.Empty)
        {
            await _utilityBillService.MarkBillAsPaidInternalAsync(billId);
        }
        
        return ApiResponse<bool>.Success(true, "Payment Successfully");
    }


    public async Task<ApiResponse<List<PaymentResponse>>> GetPaymentHistoryByTenantIdAsync(Guid userId)
    {
        var payments = await _paymentRepository.GetPaymentsByTenantId(userId);
        return ApiResponse<List<PaymentResponse>>.Success(_mapper.Map<List<PaymentResponse>>(payments), "true");
    }

    public async Task<ApiResponse<List<PaymentResponse>>> GetPaymentHistoryByOwnerIdAsync(Guid ownerId)
    {
        var payments = await _paymentRepository.GetPaymentsByOwnerId(ownerId);
        
        return ApiResponse<List<PaymentResponse>>.Success(_mapper.Map<List<PaymentResponse>>(payments), "true");
    }
    

    /// <summary>
    /// Check trạng thái thanh toán của bill
    /// </summary>
    public async Task<ApiResponse<BillPaymentStatusResponse>> GetBillPaymentStatusAsync(Guid billId)
    {
        try
        {
            var payments = await _paymentRepository.GetByBillIdAsync(billId);
            
            if (payments == null || !payments.Any())
            {
                return ApiResponse<BillPaymentStatusResponse>.Success(new BillPaymentStatusResponse
                {
                    BillId = billId,
                    IsPaid = false,
                    Status = "Pending",
                    Message = "Chưa có thanh toán"
                });
            }

            // Lấy payment mới nhất
            var latestPayment = payments.OrderByDescending(p => p.TransactionDate).First();
            
            return ApiResponse<BillPaymentStatusResponse>.Success(new BillPaymentStatusResponse
            {
                BillId = billId,
                IsPaid = true,
                Status = "Success",
                PaymentId = latestPayment.Id,
                TransactionId = latestPayment.TransactionId,
                PaidAmount = latestPayment.TransferAmount,
                PaidDate = latestPayment.TransactionDate,
                Message = "Đã thanh toán"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting bill payment status for {billId}");
            return ApiResponse<BillPaymentStatusResponse>.Fail($"Lỗi: {ex.Message}");
        }
    }
    
      private Guid ExtractBillIdFromContent(string content)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Content is null or empty");
                return Guid.Empty;
            }

            _logger.LogInformation($"🔍 Extracting BillId from content: {content}");

            // STRATEGY 1: Tìm GUID với dấu gạch ngang (format chuẩn)
            // Pattern: 8-4-4-4-12 characters (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
            var guidWithHyphensPattern = @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";
            var matchWithHyphens = System.Text.RegularExpressions.Regex.Match(content, guidWithHyphensPattern);
            
            if (matchWithHyphens.Success)
            {
                if (Guid.TryParse(matchWithHyphens.Value, out var billIdWithHyphens))
                {
                    _logger.LogInformation($"✅ Extracted BillId (with hyphens): {billIdWithHyphens}");
                    return billIdWithHyphens;
                }
            }

            // STRATEGY 2: Tìm GUID với dấu cách thay vì dấu gạch ngang
            // Pattern: 8 4 4 4 12 (có thể có space hoặc không có gì)
            // Example: "83acf6f6 4f3e 430a 9066 6351420f267c"
            var guidWithSpacesPattern = @"([0-9a-fA-F]{8})\s+([0-9a-fA-F]{4})\s+([0-9a-fA-F]{4})\s+([0-9a-fA-F]{4})\s+([0-9a-fA-F]{12})";
            var matchWithSpaces = System.Text.RegularExpressions.Regex.Match(content, guidWithSpacesPattern);
            
            if (matchWithSpaces.Success)
            {
                var guidString = $"{matchWithSpaces.Groups[1].Value}-{matchWithSpaces.Groups[2].Value}-{matchWithSpaces.Groups[3].Value}-{matchWithSpaces.Groups[4].Value}-{matchWithSpaces.Groups[5].Value}";
                
                if (Guid.TryParse(guidString, out var billIdFromSpaces))
                {
                    _logger.LogInformation($"✅ Extracted BillId (from spaces): {billIdFromSpaces}");
                    return billIdFromSpaces;
                }
            }

            // STRATEGY 3: Tìm tất cả chuỗi 32 ký tự hex liên tiếp và validate
            var normalizedContent = content.Replace(" ", "").Replace("-", "").ToUpper();
            var guidPattern = @"[0-9A-F]{32}";
            var matches = System.Text.RegularExpressions.Regex.Matches(normalizedContent, guidPattern);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var rawGuidString = match.Value;
                if (Guid.TryParseExact(rawGuidString, "N", out var billId))
                {
                    // Validate: GUID không nên bắt đầu bằng số thuần túy (avoid phone numbers, transaction codes)
                    var guidStr = billId.ToString();
                    if (!System.Text.RegularExpressions.Regex.IsMatch(guidStr, @"^[0-9]{8}-[0-9]{4}"))
                    {
                        _logger.LogInformation($"✅ Extracted BillId (32 chars): {billId}");
                        return billId;
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ Skipping invalid GUID (all numbers): {billId}");
                    }
                }
            }
            
            _logger.LogWarning($"⚠️ No valid GUID found in content: {content}");
            return Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error extracting BillId from content: {content}");
            return Guid.Empty;
        }
    }
}

    
    
    // public async Task<ApiResponse<RevenueStatsResponse>> GetSystemRevenueStatsAsync()
    // {
    //         var payments = await _paymentRepository.GetAllPaymentsAsync();
    //         var stats = CalculateRevenueStats(payments);
    //         return ApiResponse<RevenueStatsResponse>.Success(stats);
    // }
    //
    //
    //
    // public async Task<ApiResponse<RevenueStatsResponse>> GetOwnerRevenueStatsAsync(Guid ownerId, int? year)
    // {
    //    
    //         var payments = await _paymentRepository.GetPaymentsByOwnerId(ownerId);
    //
    //         // 2. Nếu có truyền Year, thì lọc list này trước
    //         if (year.HasValue)
    //         {
    //             payments = payments.Where(p => p.TransactionDate.Year == year.Value).ToList();
    //         }
    //
    //         // 3. Tính toán (Hàm cũ vẫn dùng được)
    //       //  var stats = CalculateRevenueStats(payments);
    //
    //         return ApiResponse<RevenueStatsResponse>.Success(stats);
    // }

    // private RevenueStatsResponse CalculateRevenueStats(List<Payment> payments)
    // {
    //     var response = new RevenueStatsResponse();
    //
    //     if (payments == null || !payments.Any())
    //         return response;
    //
    //     response.TotalRevenue = payments.Sum(p => p.TransferAmount);
    //     response.TotalTransactions = payments.Count;
    //
    //     // DAILY
    //     response.DailyStats = payments
    //         .GroupBy(p => new { p.TransactionDate.Year, p.TransactionDate.Month, p.TransactionDate.Day })
    //         .Select(g => new DailyRevenueStats
    //         {
    //             Year = g.Key.Year,
    //             Month = g.Key.Month,
    //             Day = g.Key.Day,
    //             Revenue = g.Sum(x => x.TransferAmount),
    //             TransactionCount = g.Count()
    //         })
    //         .OrderByDescending(d => d.Year)
    //         .ThenByDescending(d => d.Month)
    //         .ThenByDescending(d => d.Day)
    //         .ToList();
    //
    //     // MONTHLY
    //     response.MonthlyStats = payments
    //         .GroupBy(p => new { p.TransactionDate.Year, p.TransactionDate.Month })
    //         .Select(g => new MonthlyRevenueStats
    //         {
    //             Year = g.Key.Year,
    //             Month = g.Key.Month,
    //             Revenue = g.Sum(x => x.TransferAmount),
    //             TransactionCount = g.Count()
    //         })
    //         .OrderByDescending(m => m.Year)
    //         .ThenByDescending(m => m.Month)
    //         .ToList();
    //
    //     // YEARLY
    //     response.YearlyStats = payments
    //         .GroupBy(p => p.TransactionDate.Year)
    //         .Select(g => new YearlyRevenueStats
    //         {
    //             Year = g.Key,
    //             Revenue = g.Sum(x => x.TransferAmount),
    //             TransactionCount = g.Count()
    //         })
    //         .OrderByDescending(y => y.Year)
    //         .ToList();
    //
    //     return response;
    // }

    
//     private Guid ExtractBillIdFromContent(string content)
//     {
//         try
//         {
//             // Validate input
//             if (string.IsNullOrWhiteSpace(content))
//             {
//                 _logger.LogWarning("Content is null or empty");
//                 return Guid.Empty;
//             }
//
//             // 1. CHUẨN HÓA DỮ LIỆU ĐẦU VÀO
//             var normalizedContent = content
//                 .Replace(" ", "") 
//                 .Replace("-", "") 
//                 .ToUpper();
//
//             // 2. TÌM KIẾM CHUỖI GUID 32 KÝ TỰ (Định dạng N - Numeric)
//             var guidPattern = @"[0-9A-F]{32}"; 
//     
//             var match = System.Text.RegularExpressions.Regex.Match(normalizedContent, guidPattern);
//             if (match.Success)
//             {
//                 var rawGuidString = match.Groups[0].Value;
//                 if (Guid.TryParseExact(rawGuidString, "N", out var billId))
//                 {
//                     _logger.LogInformation($"✅ Extracted BillId: {billId} from content: {content}");
//                     return billId;
//                 }
//             }
//             
//             _logger.LogWarning($"⚠️ No valid GUID found in content: {content}");
//             return Guid.Empty;
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, $"❌ Error extracting BillId from content: {content}");
//             return Guid.Empty;
//         }
//     }
// }
