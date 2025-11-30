using AutoMapper;
using PaymentAPI.DTOs.Requests;
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
    private readonly IPaymentHistoryRepository _historyRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IBankGatewayRepository _bankGatewayRepository;
    private readonly IUtilityBillService _utilityBillService;
    private readonly ILogger<PaymentService> _logger;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPaymentHistoryRepository historyRepository,
        IBankAccountRepository bankAccountRepository,
        IBankGatewayRepository bankGatewayRepository,
        IUtilityBillService utilityBillService,
        ILogger<PaymentService> logger,
        IMapper mapper,
        IConfiguration configuration)
    {
        _paymentRepository = paymentRepository;
        _historyRepository = historyRepository;
        _bankAccountRepository = bankAccountRepository;
        _bankGatewayRepository = bankGatewayRepository;
        _utilityBillService = utilityBillService;
        _logger = logger;
        _mapper = mapper;
        _configuration = configuration;
    }

    #region Create Payment

    public async Task<ApiResponse<PaymentResponse>> CreatePaymentAsync(CreatePaymentRequest request, Guid tenantId)
    {
        try
        {
            // Get bill info
            var bill = await _utilityBillService.GetBillByIdAsync(request.UtilityBillId);
            if (bill == null)
            {
                return ApiResponse<PaymentResponse>.Fail("Không tìm thấy hóa đơn");
            }

            // Create payment
            var payment = new Payment
            {
                UtilityBillId = request.UtilityBillId,
                TenantId = tenantId,
                OwnerId = bill.OwnerId,
                Amount = bill.TotalAmount,
                PaymentMethod = request.PaymentMethod,
                Status = request.PaymentMethod == PaymentMethod.Online 
                    ? PaymentStatus.Processing 
                    : PaymentStatus.PendingApproval,
                PaymentCode = GeneratePaymentCode(request.UtilityBillId)
            };

            await _paymentRepository.CreateAsync(payment);

            // Log history
            await CreatePaymentHistoryAsync(payment, PaymentStatus.Pending, payment.Status, "Tạo thanh toán mới");

            return ApiResponse<PaymentResponse>.Success(_mapper.Map<PaymentResponse>(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            return ApiResponse<PaymentResponse>.Fail($"Lỗi tạo thanh toán: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PaymentResponse>> CreateOfflinePaymentAsync(Guid billId, Guid tenantId, string? notes)
    {
        try
        {
            var bill = await _utilityBillService.GetBillByIdAsync(billId);
            if (bill == null)
            {
                return ApiResponse<PaymentResponse>.Fail("Không tìm thấy hóa đơn");
            }

            var payment = new Payment
            {
                UtilityBillId = billId,
                TenantId = tenantId,
                OwnerId = bill.OwnerId,
                Amount = bill.TotalAmount,
                PaymentMethod = PaymentMethod.Offline,
                Status = PaymentStatus.PendingApproval,
                Notes = notes,
                PaymentCode = GeneratePaymentCode(billId)
            };

            await _paymentRepository.CreateAsync(payment);
            await CreatePaymentHistoryAsync(payment, PaymentStatus.Pending, PaymentStatus.PendingApproval, "Tạo thanh toán offline - chờ duyệt");

            return ApiResponse<PaymentResponse>.Success(_mapper.Map<PaymentResponse>(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating offline payment");
            return ApiResponse<PaymentResponse>.Fail($"Lỗi: {ex.Message}");
        }
    }

    #endregion

    #region QR Payment

    public async Task<ApiResponse<PaymentQRInfoResponse>> GetPaymentQRInfoAsync(Guid billId, Guid tenantId)
    {
        try
        {
            // Get bill info
            var bill = await _utilityBillService.GetBillByIdAsync(billId);
            if (bill == null)
            {
                return ApiResponse<PaymentQRInfoResponse>.Fail("Không tìm thấy hóa đơn");
            }

            // Get owner's bank account
            var bankAccounts = _bankAccountRepository.GetDefaultByUserId(bill.OwnerId).ToList();
            if (!bankAccounts.Any())
            {
                return ApiResponse<PaymentQRInfoResponse>.Fail("Chủ trọ chưa cấu hình tài khoản ngân hàng");
            }

            var bankAccount = bankAccounts.First();
            var bankGateway = await _bankGatewayRepository.GetByIdAsync(bankAccount.BankGatewayId);

            // Generate payment code
            var paymentCode = GeneratePaymentCode(billId);
            var paymentContent = $"EZSTAY {paymentCode}";

            // Generate QR URL using VietQR
            var qrUrl = GenerateVietQRUrl(
                bankGateway?.BankName ?? "MB",
                bankAccount.AccountNumber,
                bill.TotalAmount,
                paymentContent
            );

            // Check if payment already exists, if not create one
            var existingPayment = await _paymentRepository.GetByBillIdAndStatusAsync(
                billId, PaymentStatus.Pending, PaymentStatus.Processing);
            
            if (existingPayment == null)
            {
                var payment = new Payment
                {
                    UtilityBillId = billId,
                    TenantId = tenantId,
                    OwnerId = bill.OwnerId,
                    Amount = bill.TotalAmount,
                    PaymentMethod = PaymentMethod.Online,
                    Status = PaymentStatus.Processing,
                    PaymentCode = paymentCode,
                    QrDataUrl = qrUrl,
                    BankAccountNumber = bankAccount.AccountNumber
                };
                await _paymentRepository.CreateAsync(payment);
                await CreatePaymentHistoryAsync(payment, PaymentStatus.Pending, PaymentStatus.Processing, "Tạo QR thanh toán");
            }

            return ApiResponse<PaymentQRInfoResponse>.Success(new PaymentQRInfoResponse
            {
                BillId = billId,
                Amount = bill.TotalAmount,
                QrDataUrl = qrUrl,
                AccountNumber = bankAccount.AccountNumber,
                AccountName = bankAccount.Description ?? "Chủ tài khoản",
                BankName = bankGateway?.BankName ?? "Ngân hàng",
                PaymentContent = paymentContent,
                PaymentCode = paymentCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment QR info");
            return ApiResponse<PaymentQRInfoResponse>.Fail($"Lỗi: {ex.Message}");
        }
    }

    private string GenerateVietQRUrl(string bankCode, string accountNumber, decimal amount, string content)
    {
        // VietQR URL format
        return $"https://img.vietqr.io/image/{bankCode}-{accountNumber}-compact2.png?amount={amount}&addInfo={Uri.EscapeDataString(content)}";
    }

    #endregion

    #region SePay Webhook Handler - CORE LOGIC

    public async Task<ApiResponse<object>> HandleSePayWebhookAsync(
        string accountNumber, 
        decimal amount, 
        string content, 
        string transactionId)
    {
        try
        {
            _logger.LogInformation($"🔔 Processing webhook: Account={accountNumber}, Amount={amount}, Content={content}, TxId={transactionId}");

            // 1. Check duplicate transaction
            if (await _historyRepository.ExistsByTransactionIdAsync(transactionId))
            {
                _logger.LogWarning($"⚠️ Duplicate transaction: {transactionId}");
                return ApiResponse<object>.Success(new { message = "Transaction already processed" });
            }

            // 2. Parse payment code from content (format: "EZSTAY XXXXXX")
            var paymentCode = ExtractPaymentCode(content);
            if (string.IsNullOrEmpty(paymentCode))
            {
                _logger.LogWarning($"⚠️ Cannot extract payment code from content: {content}");
                
                // Still save the transaction for reference
                await SaveUnmatchedTransaction(accountNumber, amount, content, transactionId);
                
                return ApiResponse<object>.Success(new { message = "Payment code not found in content" });
            }

            _logger.LogInformation($"📝 Extracted payment code: {paymentCode}");

            // 3. Find payment by code
            var payment = await _paymentRepository.GetByPaymentCodeAsync(paymentCode);
            if (payment == null)
            {
                _logger.LogWarning($"⚠️ Payment not found for code: {paymentCode}");
                await SaveUnmatchedTransaction(accountNumber, amount, content, transactionId);
                return ApiResponse<object>.Fail($"Không tìm thấy payment với code: {paymentCode}");
            }

            // 4. Validate amount
            if (payment.Amount != amount)
            {
                _logger.LogWarning($"⚠️ Amount mismatch. Expected: {payment.Amount}, Got: {amount}");
                // Still process but log warning
            }

            // 5. Update payment status
            var previousStatus = payment.Status;
            payment.Status = PaymentStatus.Success;
            payment.TransactionId = transactionId;
            payment.TransactionContent = content;
            payment.PaidAt = DateTime.UtcNow;
            payment.Gateway = "SePay";

            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation($"✅ Payment {payment.Id} updated to Success");

            // 6. Create payment history
            var history = new PaymentHistory
            {
                PaymentId = payment.Id,
                UtilityBillId = payment.UtilityBillId,
                SePayTransactionId = transactionId,
                Amount = amount,
                TransactionContent = content,
                AccountNumber = accountNumber,
                Gateway = "SePay",
                TransferType = "in",
                PreviousStatus = previousStatus,
                NewStatus = PaymentStatus.Success,
                StatusChangeReason = "Webhook xác nhận thanh toán thành công",
                TransactionDate = DateTime.UtcNow,
                RawWebhookData = JsonSerializer.Serialize(new { accountNumber, amount, content, transactionId })
            };

            await _historyRepository.CreateAsync(history);

            _logger.LogInformation($"📝 Payment history created: {history.Id}");

            // 7. Update bill status
            var billUpdated = await _utilityBillService.UpdateBillStatusAsync(
                payment.UtilityBillId, 
                "Paid", 
                DateTime.UtcNow);

            if (billUpdated)
            {
                _logger.LogInformation($"✅ Bill {payment.UtilityBillId} marked as paid");
            }
            else
            {
                _logger.LogWarning($"⚠️ Failed to update bill {payment.UtilityBillId} status");
            }

            return ApiResponse<object>.Success(new
            {
                success = true,
                paymentId = payment.Id,
                billId = payment.UtilityBillId,
                transactionId = transactionId,
                amount = amount,
                message = "Thanh toán thành công"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SePay webhook");
            return ApiResponse<object>.Fail($"Lỗi xử lý webhook: {ex.Message}");
        }
    }

    private string? ExtractPaymentCode(string content)
    {
        // Pattern: EZSTAY followed by payment code
        // Example: "EZSTAY ABC123" or "Thanh toan EZSTAY ABC123"
        content = content.ToUpper();
        
        var patterns = new[]
        {
            @"EZSTAY\s*([A-Z0-9]+)",
            @"EZ\s*STAY\s*([A-Z0-9]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(content, pattern);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        // Try to find any GUID-like pattern
        var guidPattern = @"([A-F0-9]{8})";
        var guidMatch = System.Text.RegularExpressions.Regex.Match(content, guidPattern);
        if (guidMatch.Success)
        {
            return guidMatch.Groups[1].Value;
        }

        return null;
    }

    private async Task SaveUnmatchedTransaction(string accountNumber, decimal amount, string content, string transactionId)
    {
        try
        {
            var history = new PaymentHistory
            {
                PaymentId = Guid.Empty,
                UtilityBillId = Guid.Empty,
                SePayTransactionId = transactionId,
                Amount = amount,
                TransactionContent = content,
                AccountNumber = accountNumber,
                Gateway = "SePay",
                TransferType = "in",
                PreviousStatus = PaymentStatus.Pending,
                NewStatus = PaymentStatus.Pending,
                StatusChangeReason = "Giao dịch không khớp với payment nào",
                TransactionDate = DateTime.UtcNow,
                RawWebhookData = JsonSerializer.Serialize(new { accountNumber, amount, content, transactionId })
            };

            await _historyRepository.CreateAsync(history);
            _logger.LogInformation($"📝 Saved unmatched transaction: {transactionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving unmatched transaction");
        }
    }

    #endregion

    #region Verify & Check Payment

    public async Task<ApiResponse<PaymentResponse>> VerifyOnlinePaymentAsync(VerifyOnlinePaymentRequest request)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
            if (payment == null)
            {
                return ApiResponse<PaymentResponse>.Fail("Không tìm thấy payment");
            }

            // Just check if payment is already successful (updated by webhook)
            if (payment.Status == PaymentStatus.Success)
            {
                return ApiResponse<PaymentResponse>.Success(_mapper.Map<PaymentResponse>(payment), "Thanh toán đã được xác nhận");
            }

            return ApiResponse<PaymentResponse>.Fail("Thanh toán chưa được xác nhận. Vui lòng đợi webhook từ SePay.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment");
            return ApiResponse<PaymentResponse>.Fail($"Lỗi: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PaymentStatusResponse>> CheckPaymentStatusAsync(Guid paymentId)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                return ApiResponse<PaymentStatusResponse>.Fail("Không tìm thấy payment");
            }

            return ApiResponse<PaymentStatusResponse>.Success(new PaymentStatusResponse
            {
                PaymentId = payment.Id,
                Status = payment.Status,
                IsPaid = payment.Status == PaymentStatus.Success,
                TransactionId = payment.TransactionId,
                PaidAt = payment.PaidAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment status");
            return ApiResponse<PaymentStatusResponse>.Fail($"Lỗi: {ex.Message}");
        }
    }

    public async Task<ApiResponse<BillPaymentStatusResponse>> GetBillPaymentStatusAsync(Guid billId)
    {
        try
        {
            var payment = await _paymentRepository.GetLatestByBillIdAsync(billId);
            
            if (payment == null)
            {
                return ApiResponse<BillPaymentStatusResponse>.Success(new BillPaymentStatusResponse
                {
                    BillId = billId,
                    IsPaid = false,
                    Status = "Pending",
                    Message = "Chưa có thanh toán"
                });
            }

            return ApiResponse<BillPaymentStatusResponse>.Success(new BillPaymentStatusResponse
            {
                BillId = billId,
                IsPaid = payment.Status == PaymentStatus.Success,
                Status = payment.Status.ToString(),
                PaymentId = payment.Id,
                TransactionId = payment.TransactionId,
                PaidAmount = payment.Amount,
                PaidDate = payment.PaidAt,
                Message = payment.Status == PaymentStatus.Success ? "Đã thanh toán" : "Chưa thanh toán"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bill payment status");
            return ApiResponse<BillPaymentStatusResponse>.Fail($"Lỗi: {ex.Message}");
        }
    }

    #endregion

    #region Get Payment Info

    public async Task<ApiResponse<PaymentResponse>> GetPaymentByIdAsync(Guid paymentId)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                return ApiResponse<PaymentResponse>.Fail("Không tìm thấy payment");
            }

            return ApiResponse<PaymentResponse>.Success(_mapper.Map<PaymentResponse>(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment");
            return ApiResponse<PaymentResponse>.Fail($"Lỗi: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<PaymentResponse>>> GetPaymentsByBillIdAsync(Guid billId)
    {
        try
        {
            var payments = await _paymentRepository.GetByBillIdAsync(billId);
            return ApiResponse<List<PaymentResponse>>.Success(_mapper.Map<List<PaymentResponse>>(payments));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments by bill");
            return ApiResponse<List<PaymentResponse>>.Fail($"Lỗi: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<PaymentResponse>>> GetPaymentsByUserIdAsync(Guid userId)
    {
        try
        {
            var payments = await _paymentRepository.GetByUserIdAsync(userId);
            return ApiResponse<List<PaymentResponse>>.Success(_mapper.Map<List<PaymentResponse>>(payments));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user payments");
            return ApiResponse<List<PaymentResponse>>.Fail($"Lỗi: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PaymentResponse>> GetLatestPaymentByBillIdAsync(Guid billId)
    {
        try
        {
            var payment = await _paymentRepository.GetLatestByBillIdAsync(billId);
            if (payment == null)
            {
                return ApiResponse<PaymentResponse>.Fail("Không tìm thấy payment");
            }

            return ApiResponse<PaymentResponse>.Success(_mapper.Map<PaymentResponse>(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest payment");
            return ApiResponse<PaymentResponse>.Fail($"Lỗi: {ex.Message}");
        }
    }

    #endregion

    #region Approve Offline Payment

    public async Task<ApiResponse<PaymentResponse>> ApproveOfflinePaymentAsync(
        Guid paymentId, 
        ApprovePaymentRequest request, 
        Guid ownerId)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                return ApiResponse<PaymentResponse>.Fail("Không tìm thấy payment");
            }

            if (payment.OwnerId != ownerId)
            {
                return ApiResponse<PaymentResponse>.Fail("Bạn không có quyền duyệt payment này");
            }

            if (payment.Status != PaymentStatus.PendingApproval)
            {
                return ApiResponse<PaymentResponse>.Fail("Payment không ở trạng thái chờ duyệt");
            }

            var previousStatus = payment.Status;
            
            if (request.IsApproved)
            {
                payment.Status = PaymentStatus.Success;
                payment.ApprovedBy = ownerId;
                payment.ApprovedAt = DateTime.UtcNow;
                payment.ApprovalNotes = request.Notes;
                payment.PaidAt = DateTime.UtcNow;

                // Update bill status
                await _utilityBillService.UpdateBillStatusAsync(payment.UtilityBillId, "Paid", DateTime.UtcNow);
            }
            else
            {
                payment.Status = PaymentStatus.Rejected;
                payment.ApprovalNotes = request.Notes;
            }

            await _paymentRepository.UpdateAsync(payment);
            await CreatePaymentHistoryAsync(payment, previousStatus, payment.Status, 
                request.IsApproved ? "Chủ trọ duyệt thanh toán" : "Chủ trọ từ chối thanh toán");

            return ApiResponse<PaymentResponse>.Success(_mapper.Map<PaymentResponse>(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving payment");
            return ApiResponse<PaymentResponse>.Fail($"Lỗi: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<PaymentResponse>>> GetPendingApprovalsAsync(Guid ownerId)
    {
        try
        {
            var payments = await _paymentRepository.GetPendingApprovalsByOwnerIdAsync(ownerId);
            return ApiResponse<List<PaymentResponse>>.Success(_mapper.Map<List<PaymentResponse>>(payments));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals");
            return ApiResponse<List<PaymentResponse>>.Fail($"Lỗi: {ex.Message}");
        }
    }

    #endregion

    #region Payment History

    public async Task<ApiResponse<List<PaymentHistoryResponse>>> GetPaymentHistoryAsync(Guid paymentId)
    {
        try
        {
            var histories = await _historyRepository.GetByPaymentIdAsync(paymentId);
            return ApiResponse<List<PaymentHistoryResponse>>.Success(_mapper.Map<List<PaymentHistoryResponse>>(histories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment history");
            return ApiResponse<List<PaymentHistoryResponse>>.Fail($"Lỗi: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<PaymentHistoryResponse>>> GetBillPaymentHistoryAsync(Guid billId)
    {
        try
        {
            var histories = await _historyRepository.GetByBillIdAsync(billId);
            return ApiResponse<List<PaymentHistoryResponse>>.Success(_mapper.Map<List<PaymentHistoryResponse>>(histories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bill payment history");
            return ApiResponse<List<PaymentHistoryResponse>>.Fail($"Lỗi: {ex.Message}");
        }
    }

    private async Task CreatePaymentHistoryAsync(Payment payment, PaymentStatus previousStatus, PaymentStatus newStatus, string reason)
    {
        try
        {
            var history = new PaymentHistory
            {
                PaymentId = payment.Id,
                UtilityBillId = payment.UtilityBillId,
                Amount = payment.Amount,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                StatusChangeReason = reason,
                TransactionDate = DateTime.UtcNow
            };

            await _historyRepository.CreateAsync(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment history");
        }
    }

    #endregion

    #region Helpers

    private string GeneratePaymentCode(Guid billId)
    {
        // Generate short code from bill ID + random
        var billPart = billId.ToString("N").Substring(0, 4).ToUpper();
        var randomPart = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
        return $"{billPart}{randomPart}";
    }

    #endregion
}
