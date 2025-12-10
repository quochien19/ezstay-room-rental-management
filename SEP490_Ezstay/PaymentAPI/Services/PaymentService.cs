

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
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IUtilityBillService _utilityBillService;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IUtilityBillService utilityBillService,
        IMapper mapper,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _utilityBillService = utilityBillService;
        _mapper = mapper;
        _logger = logger;
    }



    public async Task<ApiResponse<bool>> HandleSePayWebhookAsync(CreatePayment request)
    {
       // try
      //  {
            _logger.LogInformation(
                $"🔔 Webhook received - Content: {request.Content}, Amount: {request.TransferAmount}, TransactionId: {request.TransactionId}");

            // Extract BillId from content (xử lý cả format có dấu - và không có dấu -)
            var billId = ExtractBillIdFromContent(request.Content);

            // if (billId == Guid.Empty)
            // {
            //     _logger.LogError($"❌ Cannot extract BillId from content: {request.Content}");
            //     return ApiResponse<bool>.Fail("Cannot extract BillId from payment content");
            // }

            _logger.LogInformation($"📋 BillId extracted: {billId}");

            // Get bill information - PHẢI TÌM THẤY BILL MỚI XỬ LÝ TIẾP
            var bill = await _utilityBillService.GetBillByIdAsync(billId);

            // _logger.LogInformation(
            //     $"✅ Bill found - BillId: {billId}, TenantId: {bill.TenantId}, OwnerId: {bill.OwnerId}");

            // LƯU PAYMENT CHỈ KHI TÌM THẤY BILL
            var payment = new Payment
            {
                BillId = billId,
                TenantId = bill.TenantId,
                OwnerId = bill.OwnerId,
                TransactionId = request.TransactionId,
                TransferAmount = request.TransferAmount,
                Content = request.Content,
                AccountNumber = request.AccountNumber,
                Gateway = request.Gateway ?? "SePay",
                TransferType = request.TransferType ?? "in",
                TransactionDate = DateTime.UtcNow,
            };

            await _paymentRepository.CreateAsync(payment);
            // _logger.LogInformation(
            //     $"💾 Payment saved - PaymentId: {payment.Id}, BillId: {billId}, Amount: {request.TransferAmount}");

            // Mark bill as paid
            await _utilityBillService.MarkBillAsPaidInternalAsync(billId);
          //  _logger.LogInformation($"✅ Bill marked as paid: {billId}");

            return ApiResponse<bool>.Success(true, "Payment processed and bill marked as paid successfully");
        // }
        // catch (Exception ex)
        // {
        //     _logger.LogError(ex, "❌ Error processing webhook");
        //     return ApiResponse<bool>.Fail($"Error: {ex.Message}");
        // }
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

//       private Guid ExtractBillIdFromContent(string content)
//     {
//         try
//         {
//             if (string.IsNullOrWhiteSpace(content))
//             {
//                 _logger.LogWarning("Content is null or empty");
//                 return Guid.Empty;
//             }
//
//             _logger.LogInformation($"🔍 Extracting BillId from content: {content}");
//             
//             // Tìm GUID 32 ký tự liền không có dấu gạch ngang (format từ SePay)
//             // Input: 83acf6f64f3e430a90666351420f267c
//             // Output: 83acf6f6-4f3e-430a-9066-6351420f267c
//             var guidPattern = @"[0-9a-fA-F]{32}";
//             var match = System.Text.RegularExpressions.Regex.Match(content, guidPattern);
//             
//             if (match.Success)
//             {
//                 var rawGuidString = match.Value;
//                 
//                 if (Guid.TryParseExact(rawGuidString, "N", out var billId))
//                 {
//                     _logger.LogInformation($"✅ Extracted BillId: {billId} from: {rawGuidString}");
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
//       
//       
//     
// }


    private Guid ExtractBillIdFromContent(string content)
    {


        var guidWithHyphensPattern = @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";
        var matchWithHyphens = System.Text.RegularExpressions.Regex.Match(content, guidWithHyphensPattern);

        if (matchWithHyphens.Success)
        {
            if (Guid.TryParse(matchWithHyphens.Value, out var billIdWithHyphens))
            {
                return billIdWithHyphens;
            }
        }

        var guidWithSpacesPattern =
            @"([0-9a-fA-F]{8})\s+([0-9a-fA-F]{4})\s+([0-9a-fA-F]{4})\s+([0-9a-fA-F]{4})\s+([0-9a-fA-F]{12})";
        var matchWithSpaces = System.Text.RegularExpressions.Regex.Match(content, guidWithSpacesPattern);

        if (matchWithSpaces.Success)
        {
            var guidString =
                $"{matchWithSpaces.Groups[1].Value}-{matchWithSpaces.Groups[2].Value}-{matchWithSpaces.Groups[3].Value}-{matchWithSpaces.Groups[4].Value}-{matchWithSpaces.Groups[5].Value}";

            if (Guid.TryParse(guidString, out var billIdFromSpaces))
            {
                return billIdFromSpaces;
            }
        }

        var normalizedContent = content.Replace(" ", "").Replace("-", "").ToUpper();
        var guidPattern = @"[0-9A-F]{32}";
        var matches = System.Text.RegularExpressions.Regex.Matches(normalizedContent, guidPattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var rawGuidString = match.Value;
            if (Guid.TryParseExact(rawGuidString, "N", out var billId))
            {
                var guidStr = billId.ToString();
                if (!System.Text.RegularExpressions.Regex.IsMatch(guidStr, @"^[0-9]{8}-[0-9]{4}"))
                {
                    return billId;
                }
            }
        }

        return Guid.Empty;

    }
}

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
