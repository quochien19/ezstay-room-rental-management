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
    private readonly ISePayService _sePayService;
    private readonly IUtilityBillService _utilityBillService;
    private readonly ILogger<PaymentService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IBankAccountRepository bankAccountRepository,
        ISePayService sePayService,
        IUtilityBillService utilityBillService,
        ILogger<PaymentService> logger,
        HttpClient httpClient,
        IConfiguration configuration,
        IMapper mapper)
    {
        _paymentRepository = paymentRepository;
        _bankAccountRepository = bankAccountRepository;
        _sePayService = sePayService;
        _utilityBillService = utilityBillService;
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PaymentResponse>> CreatePaymentAsync(CreatePaymentRequest request, Guid tenantId)
    {
        try
        {
            // Lấy thông tin bill
            var bill = await _utilityBillService.GetBillByIdAsync(request.UtilityBillId);
            if (bill == null)
            {
                return ApiResponse<PaymentResponse>.Fail("Không tìm thấy hóa đơn");
            }

            // // Kiểm tra xem bill đã được thanh toán chưa
            // if (bill.PaymentDate.HasValue)
            // {
            //     return ApiResponse<PaymentResponse>.Fail("Hóa đơn đã được thanh toán");
            // }

            // Lấy thông tin tài khoản ngân hàng của chủ trọ
            Console.WriteLine("ssss +"+ bill.OwnerId);
            // var bankAccount = await _bankAccountRepository.GetDefaultByUserId(bill.OwnerId);
            // if (bankAccount == null)
            // {
            //     return ApiResponse<PaymentResponse>.Fail("Chủ trọ chưa thiết lập tài khoản ngân hàng");
            // }
            
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UtilityBillId = request.UtilityBillId,
                TenantId = tenantId,
                OwnerId = bill.OwnerId,
                // BankAccountId = bankAccount.Id,
                // BankAccountNumber = bankAccount.AccountNumber,
                Amount = bill.TotalAmount,
                PaymentMethod = request.PaymentMethod,
                Status = PaymentStatus.Pending,
                CreatedDate = DateTime.UtcNow,
            };

            await _paymentRepository.Add(payment);

            // Nếu là online payment, trả về thông tin chuyển khoản
            if (request.PaymentMethod == PaymentMethod.Online)
            {
                var transactionContent = $"THANHTOAN {payment.Id.ToString().Substring(0, 8).ToUpper()}";
                
                var response = new PaymentResponse
                {
                    PaymentId = payment.Id.ToString(),
                    Status = "Pending",
                    Message = "Vui lòng chuyển khoản theo thông tin bên dưới",
                    PaymentInstruction = new PaymentInstructionDto
                    {
                        // BankName = bankAccount.BankName,
                        // AccountNumber = bankAccount.AccountNumber,
                        AccountName = "Chủ trọ", 
                        Amount = bill.TotalAmount,
                        TransactionContent = transactionContent,
                       // QRCodeUrl = bankAccount.ImageQR // URL QR code từ bank account
                    }
                };
                
                return ApiResponse<PaymentResponse>.Success(response, "Tạo payment thành công");
            }

            // Offline payment
            var offlineResponse = new PaymentResponse
            {
                PaymentId = payment.Id.ToString(),
                Status = "Pending",
                Message = "Vui lòng thanh toán trực tiếp"
            };
            
            return ApiResponse<PaymentResponse>.Success(offlineResponse, "Tạo payment thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            return ApiResponse<PaymentResponse>.Fail($"Lỗi khi tạo payment: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PaymentResponse>> VerifyOnlinePaymentAsync(VerifyOnlinePaymentRequest request)
    {
        try
        {
            var payment = await _paymentRepository.GetById(request.PaymentId);
            if (payment == null)
            {
                return ApiResponse<PaymentResponse>.Fail("Không tìm thấy payment");
            }

            if (payment.Status == PaymentStatus.Success)
            {
                var completedResponse = new PaymentResponse
                {
                    PaymentId = payment.Id.ToString(),
                    Status = "Completed",
                    Message = "Payment đã được xác nhận trước đó"
                };
                return ApiResponse<PaymentResponse>.Success(completedResponse);
            }

            if (payment.PaymentMethod != PaymentMethod.Online)
            {
                return ApiResponse<PaymentResponse>.Fail("Chỉ có thể verify online payment");
            }

            // Kiểm tra xem transaction này đã được sử dụng chưa
            var existingPayment = await _paymentRepository.GetByTransactionId(request.TransactionId);
            if (existingPayment != null && existingPayment.Id != payment.Id)
            {
                return ApiResponse<PaymentResponse>.Fail("Mã giao dịch này đã được sử dụng cho payment khác");
            }

            // Tạo transaction content để verify
            var expectedContent = $"THANHTOAN {payment.Id.ToString().Substring(0, 8).ToUpper()}";

            // Verify với SePay
            var isValid = await _sePayService.VerifyTransactionAsync(
                request.TransactionId,
                payment.Amount,
                expectedContent,
                payment.BankAccountNumber!
            );

            if (!isValid)
            {
                var failedResponse = new PaymentResponse
                {
                    PaymentId = payment.Id.ToString(),
                    Status = "Failed",
                    Message = "Không thể xác thực giao dịch. Vui lòng kiểm tra lại mã giao dịch, số tiền và nội dung chuyển khoản."
                };
                return ApiResponse<PaymentResponse>.Success(failedResponse);
            }

            // Lấy chi tiết transaction từ SePay
            var transactionDetails = await _sePayService.GetTransactionDetailsAsync(request.TransactionId);
            
            // Update payment
            payment.Status = PaymentStatus.Success;
            payment.TransactionId = request.TransactionId;
            payment.CompletedDate = DateTime.UtcNow;
            payment.UpdatedDate = DateTime.UtcNow;
            
            if (transactionDetails?.Data != null)
            {
                payment.TransactionContent = transactionDetails.Data.Description;
                payment.BankBrandName = transactionDetails.Data.BankBrandName;
                payment.TransactionDate = DateTime.Parse(transactionDetails.Data.TransactionDate);
                payment.SePayResponse = JsonSerializer.Serialize(transactionDetails);
            }

            await _paymentRepository.Update(payment);

            // Update bill status
            await _utilityBillService.UpdateBillStatusAsync(
                payment.UtilityBillId,
                "Paid",
                DateTime.UtcNow
            );

            var successResponse = new PaymentResponse
            {
                PaymentId = payment.Id.ToString(),
                Status = "Completed",
                Message = "Thanh toán thành công!"
            };
            
            return ApiResponse<PaymentResponse>.Success(successResponse, "Thanh toán thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying online payment");
            return ApiResponse<PaymentResponse>.Fail($"Lỗi khi xác thực thanh toán: {ex.Message}");
        }
    }

    public async Task<ApiResponse<string>> UploadReceiptImageAsync(Guid paymentId, Stream fileStream, string fileName)
    {
        try
        {
            var payment = await _paymentRepository.GetById(paymentId);
            if (payment == null)
            {
                return ApiResponse<string>.Fail("Không tìm thấy payment");
            }

            if (payment.PaymentMethod != PaymentMethod.Offline)
            {
                return ApiResponse<string>.Fail("Chỉ offline payment mới cần upload receipt");
            }

            // TODO: Upload to Image API
            var imageApiUrl = _configuration["ServiceUrls:ImageApi"];
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", fileName);

            var response = await _httpClient.PostAsync($"{imageApiUrl}api/images/upload", content);
            if (!response.IsSuccessStatusCode)
            {
                return ApiResponse<string>.Fail("Không thể upload ảnh");
            }

            var result = await response.Content.ReadAsStringAsync();
            var imageUrl = JsonSerializer.Deserialize<JsonElement>(result).GetProperty("url").GetString();

            if (string.IsNullOrEmpty(imageUrl))
            {
                return ApiResponse<string>.Fail("Không nhận được URL ảnh");
            }

            payment.ReceiptImageUrl = imageUrl;
            payment.UpdatedDate = DateTime.UtcNow;
            await _paymentRepository.Update(payment);

            return ApiResponse<string>.Success(imageUrl, "Upload ảnh biên lai thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading receipt image");
            return ApiResponse<string>.Fail($"Lỗi khi upload ảnh: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PaymentDetailResponse>> GetPaymentByIdAsync(Guid paymentId)
    {
        try
        {
            var payment = await _paymentRepository.GetById(paymentId);
            if (payment == null)
            {
                return ApiResponse<PaymentDetailResponse>.Fail("Không tìm thấy payment");
            }

            var result = _mapper.Map<PaymentDetailResponse>(payment);
            return ApiResponse<PaymentDetailResponse>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment by id");
            return ApiResponse<PaymentDetailResponse>.Fail($"Lỗi khi lấy thông tin payment: {ex.Message}");
        }
    }

    public Task<ApiResponse<List<PaymentInfo>>> GetPaymentsByBillIdAsync(Guid billId)
    {
        try
        {
            var payments = _paymentRepository.GetByBillId(billId).ToList();
            var result = _mapper.Map<List<PaymentInfo>>(payments);
            return Task.FromResult(ApiResponse<List<PaymentInfo>>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments by bill id");
            return Task.FromResult(ApiResponse<List<PaymentInfo>>.Fail($"Lỗi khi lấy danh sách payment: {ex.Message}"));
        }
    }

    public Task<ApiResponse<List<PaymentInfo>>> GetPaymentsByUserIdAsync(Guid userId)
    {
        try
        {
            var payments = _paymentRepository.GetByUserId(userId).ToList();
            var result = _mapper.Map<List<PaymentInfo>>(payments);
            return Task.FromResult(ApiResponse<List<PaymentInfo>>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments by user id");
            return Task.FromResult(ApiResponse<List<PaymentInfo>>.Fail($"Lỗi khi lấy danh sách payment: {ex.Message}"));
        }
    }

    public async Task<ApiResponse<PaymentResponse>> ApproveOfflinePaymentAsync(Guid paymentId, ApprovePaymentRequest request, Guid ownerId)
    {
        try
        {
            var payment = await _paymentRepository.GetById(paymentId);
            if (payment == null)
            {
                return ApiResponse<PaymentResponse>.Fail("Không tìm thấy payment");
            }

            if (payment.OwnerId != ownerId)
            {
                return ApiResponse<PaymentResponse>.Fail("Bạn không có quyền duyệt payment này");
            }

            if (payment.PaymentMethod != PaymentMethod.Offline)
            {
                return ApiResponse<PaymentResponse>.Fail("Chỉ có thể approve offline payment");
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                return ApiResponse<PaymentResponse>.Fail("Payment không ở trạng thái chờ duyệt");
            }

            payment.Status = PaymentStatus.Success;
            payment.ApprovedBy = ownerId;
            payment.ApprovedAt = DateTime.UtcNow;
            payment.CompletedDate = DateTime.UtcNow;
            payment.UpdatedDate = DateTime.UtcNow;
            payment.Notes = request.Notes;

            await _paymentRepository.Update(payment);

            // Update bill status
            await _utilityBillService.UpdateBillStatusAsync(
                payment.UtilityBillId,
                "Paid",
                DateTime.UtcNow
            );

            var response = new PaymentResponse
            {
                PaymentId = payment.Id.ToString(),
                Status = "Approved",
                Message = "Đã duyệt payment thành công"
            };
            
            return ApiResponse<PaymentResponse>.Success(response, "Duyệt payment thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving offline payment");
            return ApiResponse<PaymentResponse>.Fail($"Lỗi khi duyệt payment: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PaymentResponse>> RejectOfflinePaymentAsync(Guid paymentId, RejectPaymentRequest request, Guid ownerId)
    {
        try
        {
            var payment = await _paymentRepository.GetById(paymentId);
            if (payment == null)
            {
                return ApiResponse<PaymentResponse>.Fail("Không tìm thấy payment");
            }

            if (payment.OwnerId != ownerId)
            {
                return ApiResponse<PaymentResponse>.Fail("Bạn không có quyền từ chối payment này");
            }

            if (payment.PaymentMethod != PaymentMethod.Offline)
            {
                return ApiResponse<PaymentResponse>.Fail("Chỉ có thể reject offline payment");
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                return ApiResponse<PaymentResponse>.Fail("Payment không ở trạng thái chờ duyệt");
            }

            payment.Status = PaymentStatus.Rejected;
            payment.RejectedBy = ownerId;
            payment.RejectedAt = DateTime.UtcNow;
            payment.RejectionReason = request.Reason;
            payment.UpdatedDate = DateTime.UtcNow;

            await _paymentRepository.Update(payment);

            var response = new PaymentResponse
            {
                PaymentId = payment.Id.ToString(),
                Status = "Rejected",
                Message = "Đã từ chối payment"
            };
            
            return ApiResponse<PaymentResponse>.Success(response, "Từ chối payment thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting offline payment");
            return ApiResponse<PaymentResponse>.Fail($"Lỗi khi từ chối payment: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<PaymentDetailResponse>>> GetPendingApprovalsAsync(Guid ownerId)
    {
        try
        {
            var payments = await _paymentRepository.GetPendingOfflinePaymentsByOwner(ownerId);
            var result = _mapper.Map<List<PaymentDetailResponse>>(payments);
            return ApiResponse<List<PaymentDetailResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals");
            return ApiResponse<List<PaymentDetailResponse>>.Fail($"Lỗi khi lấy danh sách payment chờ duyệt: {ex.Message}");
        }
    }

    public Task<ApiResponse<PaymentDetailResponse>> GetLatestPaymentByBillIdAsync(Guid billId)
    {
        try
        {
            var payment = _paymentRepository.GetByBillId(billId)
                .OrderByDescending(p => p.CreatedDate)
                .FirstOrDefault();

            if (payment == null)
            {
                return Task.FromResult(ApiResponse<PaymentDetailResponse>.Fail("Chưa có payment nào cho hóa đơn này"));
            }

            var result = _mapper.Map<PaymentDetailResponse>(payment);
            return Task.FromResult(ApiResponse<PaymentDetailResponse>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest payment by bill id");
            return Task.FromResult(ApiResponse<PaymentDetailResponse>.Fail($"Lỗi khi lấy payment: {ex.Message}"));
        }
    }

    public async Task<ApiResponse<PaymentDetailResponse>> CheckPaymentStatusAsync(Guid paymentId)
    {
        try
        {
            var payment = await _paymentRepository.GetById(paymentId);
            if (payment == null)
            {
                return ApiResponse<PaymentDetailResponse>.Fail("Không tìm thấy payment");
            }

            // Nếu payment đã completed, return luôn
            if (payment.Status == PaymentStatus.Success)
            {
                var result = _mapper.Map<PaymentDetailResponse>(payment);
                return ApiResponse<PaymentDetailResponse>.Success(result, "Payment đã được thanh toán");
            }

            // Nếu là online payment và đang pending, thử check với SePay
            if (payment.PaymentMethod == PaymentMethod.Online && payment.Status == PaymentStatus.Pending)
            {
                // Tạo expected content - có thể dùng để query SePay nếu cần
                // var expectedContent = $"THANHTOAN {payment.Id.ToString().Substring(0, 8).ToUpper()}";

                // Try to find transaction in SePay (này cần SePay có API list transactions)
                // For now, just return current status
                _logger.LogInformation($"Checking payment status for {paymentId}, status: {payment.Status}");
            }

            var currentResult = _mapper.Map<PaymentDetailResponse>(payment);
            return ApiResponse<PaymentDetailResponse>.Success(currentResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment status");
            return ApiResponse<PaymentDetailResponse>.Fail($"Lỗi khi kiểm tra trạng thái payment: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PaymentResponse>> HandleSePayWebhookAsync(
        string accountNumber, 
        decimal amount, 
        string description, 
        string transactionId)
    {
        try
        {
            _logger.LogInformation($"🔔 Received SePay webhook: AccountNumber={accountNumber}, Amount={amount}, Description={description}, TransactionId={transactionId}");

            // Parse bill ID from description - Support multiple formats
            // Formats:
            // - "THANHTOAN BILL 148A4D2E" (user input - short)
            // - "148a4d2e-8ed5-4d16-abea-10d3974e288f" (GUID with dashes)
            // - "148a4d2e8ed54d16abea10d3974e288f" (GUID without dashes - bank removes dashes)
            // - "MBVCB.xxx.Thanh toan hoa don 148a4d2e8ed54d16abea10d3974e288f.CT tu..." (bank format)
            
            _logger.LogInformation($"📝 Original description: {description}");
            
            string? billIdString = null;
            bool isBillPayment = true;  // Always treat as bill payment in new flow
            
            // Try to extract full GUID pattern WITH dashes (8-4-4-4-12 format)
            var guidWithDashPattern = @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})";
            var guidWithDashMatch = System.Text.RegularExpressions.Regex.Match(description, guidWithDashPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (guidWithDashMatch.Success)
            {
                billIdString = guidWithDashMatch.Groups[1].Value;
                _logger.LogInformation($"✅ Found full GUID with dashes: {billIdString}");
            }
            else
            {
                // Try to extract GUID WITHOUT dashes (32 hex chars - bank removes dashes)
                // Pattern: look for 32 consecutive hex characters
                var guidNoDashPattern = @"([0-9a-fA-F]{32})";
                var guidNoDashMatch = System.Text.RegularExpressions.Regex.Match(description, guidNoDashPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (guidNoDashMatch.Success)
                {
                    var rawGuid = guidNoDashMatch.Groups[1].Value;
                    // Convert "148a4d2e8ed54d16abea10d3974e288f" to "148a4d2e-8ed5-4d16-abea-10d3974e288f"
                    billIdString = $"{rawGuid.Substring(0, 8)}-{rawGuid.Substring(8, 4)}-{rawGuid.Substring(12, 4)}-{rawGuid.Substring(16, 4)}-{rawGuid.Substring(20, 12)}";
                    _logger.LogInformation($"✅ Found GUID without dashes: {rawGuid} → formatted: {billIdString}");
                }
                else
                {
                    // Try to find 8-character code (short version)
                    var shortPattern = @"(?:BILL\s+|hoa\s*don\s+|THANHTOAN\s+)([0-9a-fA-F]{8})";
                    var shortMatch = System.Text.RegularExpressions.Regex.Match(description, shortPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    
                    if (shortMatch.Success)
                    {
                        billIdString = shortMatch.Groups[1].Value;
                        _logger.LogInformation($"✅ Found short code: {billIdString}");
                    }
                }
            }

            if (string.IsNullOrEmpty(billIdString))
            {
                _logger.LogWarning($"❌ Cannot parse bill ID from description: {description}");
                return ApiResponse<PaymentResponse>.Fail("Không tìm thấy mã hóa đơn trong nội dung chuyển khoản");
            }

            string billOrPaymentCode = billIdString;
            _logger.LogInformation($"🔍 Extracted bill/payment code: {billOrPaymentCode}");

            Payment? payment = null;

            if (isBillPayment)
            {
                // NEW FLOW: Tìm bill và TẠO PAYMENT MỚI
                _logger.LogInformation($"🆕 Processing new flow - Bill code: {billOrPaymentCode}");

                // Try to parse as full GUID first
                Guid billGuid;
                if (!Guid.TryParse(billOrPaymentCode, out billGuid))
                {
                    _logger.LogWarning($"❌ Invalid GUID format: {billOrPaymentCode}");
                    return ApiResponse<PaymentResponse>.Fail($"Mã hóa đơn không hợp lệ: {billOrPaymentCode}");
                }

                _logger.LogInformation($"🔍 Parsed GUID: {billGuid}");
                _logger.LogInformation($"📞 Calling UtilityBillService.GetBillByIdAsync({billGuid})...");
                
                var bill = await _utilityBillService.GetBillByIdAsync(billGuid);
                
                if (bill != null)
                {
                    _logger.LogInformation($"✅ Bill found: {bill.Id}, Amount: {bill.TotalAmount}, Status: {bill.Status}");
                }
                else
                {
                    _logger.LogWarning($"❌ Bill is null for ID: {billGuid}");
                }
                
                if (bill == null)
                {
                    _logger.LogWarning($"❌ No bill found for ID: {billGuid}");
                    return ApiResponse<PaymentResponse>.Fail("Không tìm thấy hóa đơn tương ứng");
                }

                // Kiểm tra bill đã thanh toán chưa
                // if (bill.PaymentDate.HasValue)
                // {
                //     _logger.LogWarning($"Bill {bill.Id} already paid");
                //     return ApiResponse<PaymentResponse>.Fail("Hóa đơn đã được thanh toán");
                // }

                // Verify amount
                if (bill.TotalAmount != amount)
                {
                    _logger.LogWarning($"Amount mismatch. Expected: {bill.TotalAmount}, Got: {amount}");
                    return ApiResponse<PaymentResponse>.Fail($"Số tiền không khớp. Cần: {bill.TotalAmount}, Nhận: {amount}");
                }

                // Lấy bank account
                // var bankAccount = await _bankAccountRepository.GetDefaultByUserId(bill.OwnerId);
                // if (bankAccount == null || bankAccount.AccountNumber != accountNumber)
                // {
                //     _logger.LogWarning($"Bank account mismatch or not found");
                //     return ApiResponse<PaymentResponse>.Fail("Tài khoản ngân hàng không khớp");
                // }

                // TẠO PAYMENT MỚI (chỉ khi đã chuyển khoản thực sự)
                var newPaymentId = Guid.NewGuid();
                _logger.LogInformation($"💳 Creating new payment with ID: {newPaymentId}");
                
                payment = new Payment
                {
                    Id = newPaymentId,
                    UtilityBillId = bill.Id,
                    TenantId = bill.TenantId,
                    OwnerId = bill.OwnerId,
                    // BankAccountId = bankAccount.Id,
                    // BankAccountNumber = bankAccount.AccountNumber,
                    Amount = amount,
                    PaymentMethod = PaymentMethod.Online,
                    Status = PaymentStatus.Success, // Tạo luôn với status Success
                    TransactionId = transactionId,
                    TransactionContent = description,
                    CompletedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    TransactionDate = DateTime.UtcNow
                };

                _logger.LogInformation($"💾 Saving payment to database...");
                await _paymentRepository.Add(payment);
                _logger.LogInformation($"✅ Created new payment {payment.Id} from webhook");
            }
            else
            {
                // OLD FLOW: Tìm payment đã tạo trước (backward compatible)
                _logger.LogInformation($"Processing old flow - Payment code: {billOrPaymentCode}");

                var payments = _paymentRepository.GetByOwner(Guid.Empty)
                    .Where(p => p.Id.ToString().Substring(0, 8).ToUpper() == billOrPaymentCode.ToUpper())
                    .ToList();

                payments = payments.Where(p => p.BankAccountNumber == accountNumber).ToList();

                if (!payments.Any())
                {
                    _logger.LogWarning($"No payment found for code: {billOrPaymentCode}");
                    return ApiResponse<PaymentResponse>.Fail("Không tìm thấy payment tương ứng");
                }

                payment = payments.FirstOrDefault();
                
                if (payment == null)
                {
                    return ApiResponse<PaymentResponse>.Fail("Không tìm thấy payment");
                }

                // Kiểm tra đã completed chưa
                if (payment.Status == PaymentStatus.Success)
                {
                    var completedResponse = new PaymentResponse
                    {
                        PaymentId = payment.Id.ToString(),
                        Status = "Completed",
                        Message = "Payment đã được xác nhận trước đó"
                    };
                    return ApiResponse<PaymentResponse>.Success(completedResponse);
                }

                // Verify amount
                if (payment.Amount != amount)
                {
                    _logger.LogWarning($"Amount mismatch. Expected: {payment.Amount}, Got: {amount}");
                    return ApiResponse<PaymentResponse>.Fail($"Số tiền không khớp");
                }

                // Update payment
                payment.Status = PaymentStatus.Success;
                payment.TransactionId = transactionId;
                payment.TransactionContent = description;
                payment.CompletedDate = DateTime.UtcNow;
                payment.UpdatedDate = DateTime.UtcNow;
                payment.TransactionDate = DateTime.UtcNow;

                await _paymentRepository.Update(payment);
                _logger.LogInformation($"Updated payment {payment.Id} from webhook");
            }

            // Get full transaction details from SePay
            var transactionDetails = await _sePayService.GetTransactionDetailsAsync(transactionId);
            if (transactionDetails?.Data != null && payment != null)
            {
                payment.BankBrandName = transactionDetails.Data.BankBrandName;
                payment.SePayResponse = JsonSerializer.Serialize(transactionDetails);
                await _paymentRepository.Update(payment);
            }

            if (payment == null)
            {
                _logger.LogError("❌ Payment is null after creation/update");
                return ApiResponse<PaymentResponse>.Fail("Lỗi: Payment không được tạo");
            }

            // Update bill status
            _logger.LogInformation($"📝 Updating bill {payment.UtilityBillId} status to Paid...");
            var updateResult = await _utilityBillService.UpdateBillStatusAsync(
                payment.UtilityBillId,
                "Paid",
                DateTime.UtcNow
            );
            
            if (updateResult)
            {
                _logger.LogInformation($"✅ Bill {payment.UtilityBillId} marked as Paid successfully");
            }
            else
            {
                _logger.LogError($"❌ Failed to mark bill {payment.UtilityBillId} as Paid");
            }

            _logger.LogInformation($"🎉 Payment {payment.Id} completed successfully via webhook");

            var successResponse = new PaymentResponse
            {
                PaymentId = payment.Id.ToString(),
                Status = "Completed",
                Message = "Thanh toán thành công qua webhook!"
            };

            return ApiResponse<PaymentResponse>.Success(successResponse, "Thanh toán thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling SePay webhook");
            return ApiResponse<PaymentResponse>.Fail($"Lỗi khi xử lý webhook: {ex.Message}");
        }
    }

    /// <summary>
    /// KHÔNG TẠO PAYMENT - CHỈ LẤY THÔNG TIN QR ĐỂ HIỂN THỊ
    /// Payment chỉ được tạo khi webhook về (user đã chuyển khoản thực sự)
    /// </summary>
    public async Task<ApiResponse<PaymentQRResponse>> GetPaymentQRInfoAsync(Guid billId, Guid tenantId)
    {
        try
        {
            // Lấy thông tin bill
            var bill = await _utilityBillService.GetBillByIdAsync(billId);
            if (bill == null)
            {
                return ApiResponse<PaymentQRResponse>.Fail("Không tìm thấy hóa đơn");
            }

            // Kiểm tra bill đã thanh toán chưa
            // if (bill.PaymentDate.HasValue)
            // {
            //     return ApiResponse<PaymentQRResponse>.Fail("Hóa đơn đã được thanh toán");
            // }

            // Lấy thông tin tài khoản ngân hàng của chủ trọ
            // var bankAccount = await _bankAccountRepository.GetDefaultByUserId(bill.OwnerId);
            // if (bankAccount == null)
            // {
            //     return ApiResponse<PaymentQRResponse>.Fail("Chủ trọ chưa thiết lập tài khoản ngân hàng");
            // }

            // Tạo transaction content dựa trên billId (KHÔNG TẠO PAYMENT)
            // Format: "THANHTOAN BILL {billId-8-ký-tự}"
            var billCode = billId.ToString().Substring(0, 8).ToUpper();
            var transactionContent = $"THANHTOAN BILL {billCode}";

            var qrResponse = new PaymentQRResponse
            {
                BillId = billId.ToString(),
                Amount = bill.TotalAmount,
                // BankName = bankAccount.BankName,
                // AccountNumber = bankAccount.AccountNumber,
                // AccountName = "Chủ trọ", // TODO: Get from User API
                // TransactionContent = transactionContent,
                // QRCodeUrl = bankAccount.ImageQR
            };

            return ApiResponse<PaymentQRResponse>.Success(qrResponse, "Lấy thông tin QR thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment QR info");
            return ApiResponse<PaymentQRResponse>.Fail($"Lỗi khi lấy thông tin QR: {ex.Message}");
        }
    }

    /// <summary>
    /// Tạo payment cho thanh toán Offline (tiền mặt)
    /// Payment sẽ được tạo ngay với Status = Pending, chờ admin approve
    /// </summary>
    public async Task<ApiResponse<PaymentResponse>> CreateOfflinePaymentAsync(Guid billId, Guid tenantId, string? notes)
    {
        try
        {
            // Lấy thông tin bill
            var bill = await _utilityBillService.GetBillByIdAsync(billId);
            if (bill == null)
            {
                return ApiResponse<PaymentResponse>.Fail("Không tìm thấy hóa đơn");
            }

            // Kiểm tra xem đã có payment nào cho bill này chưa
            var existingPayments = _paymentRepository.GetByBillId(billId).ToList();
            var existingPayment = existingPayments.FirstOrDefault(p => 
                p.Status != PaymentStatus.Failed && p.Status != PaymentStatus.Rejected);
            
            if (existingPayment != null)
            {
                return ApiResponse<PaymentResponse>.Fail($"Hóa đơn này đã có thanh toán với trạng thái {existingPayment.Status.ToString()}");
            }

            // Lấy thông tin tài khoản ngân hàng của chủ trọ (optional cho offline)
         //   var bankAccount = await _bankAccountRepository.GetDefaultByUserId(bill.OwnerId);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UtilityBillId = billId,
                TenantId = tenantId,
                OwnerId = bill.OwnerId,
                // BankAccountId = bankAccount?.Id ?? Guid.Empty,
                // BankAccountNumber = bankAccount?.AccountNumber,
                Amount = bill.TotalAmount,
                PaymentMethod = PaymentMethod.Offline, // Offline payment
                Status = PaymentStatus.Pending,
                Notes = notes,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            await _paymentRepository.Add(payment);

            var response = new PaymentResponse
            {
                PaymentId = payment.Id.ToString(),
                Status = payment.Status.ToString(),
                Message = "Đã tạo thanh toán Offline. Vui lòng upload biên lai để admin xác nhận."
            };

            _logger.LogInformation($"Created offline payment {payment.Id} for bill {billId}");
            return ApiResponse<PaymentResponse>.Success(response, "Tạo thanh toán Offline thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating offline payment for bill {billId}");
            return ApiResponse<PaymentResponse>.Fail($"Lỗi khi tạo thanh toán: {ex.Message}");
        }
    }

    public async Task<ApiResponse<BillPaymentStatusResponse>> GetBillPaymentStatusAsync(Guid billId)
    {
        try
        {
            // Lấy tất cả payment của bill
            var payments = _paymentRepository.GetByOwner(Guid.Empty)
                .Where(p => p.UtilityBillId == billId)
                .OrderByDescending(p => p.CreatedDate)
                .ToList();

            if (!payments.Any())
            {
                return ApiResponse<BillPaymentStatusResponse>.Success(new BillPaymentStatusResponse
                {
                    IsPaid = false,
                    Status = "NoPament",
                    Message = "Chưa có thanh toán nào"
                });
            }

            // Lấy payment thành công gần nhất
            var successPayment = payments.FirstOrDefault(p => p.Status == PaymentStatus.Success);

            if (successPayment != null)
            {
                return ApiResponse<BillPaymentStatusResponse>.Success(new BillPaymentStatusResponse
                {
                    IsPaid = true,
                    PaymentId = successPayment.Id.ToString(),
                    PaidAmount = successPayment.Amount,
                    PaidDate = successPayment.CompletedDate,
                    TransactionId = successPayment.TransactionId,
                    Status = "Success",
                    Message = "Đã thanh toán thành công"
                });
            }

            // Check pending
            var pendingPayment = payments.FirstOrDefault(p => p.Status == PaymentStatus.Pending);
            if (pendingPayment != null)
            {
                return ApiResponse<BillPaymentStatusResponse>.Success(new BillPaymentStatusResponse
                {
                    IsPaid = false,
                    PaymentId = pendingPayment.Id.ToString(),
                    Status = "Pending",
                    Message = "Đang chờ xác nhận thanh toán"
                });
            }

            return ApiResponse<BillPaymentStatusResponse>.Success(new BillPaymentStatusResponse
            {
                IsPaid = false,
                Status = "Unknown",
                Message = "Trạng thái không xác định"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting bill payment status for {billId}");
            return ApiResponse<BillPaymentStatusResponse>.Fail($"Lỗi: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PaymentResponse>> CheckPaymentManualAsync(Guid billId, Guid tenantId)
    {
        try
        {
            _logger.LogInformation($"Manual checking payment for bill {billId} by tenant {tenantId}");

            // Lấy thông tin bill
            var bill = await _utilityBillService.GetBillByIdAsync(billId);
            if (bill == null)
            {
                return ApiResponse<PaymentResponse>.Fail("Không tìm thấy hóa đơn");
            }

            // Verify tenant ownership (skip if tenantId is empty for testing)
            if (tenantId != Guid.Empty && bill.TenantId != tenantId)
            {
                _logger.LogWarning($"⚠️ Tenant ID mismatch: Bill tenant={bill.TenantId}, Request tenant={tenantId}");
                return ApiResponse<PaymentResponse>.Fail("Bạn không có quyền kiểm tra hóa đơn này");
            }
            
            if (tenantId == Guid.Empty)
            {
                _logger.LogInformation($"ℹ️ Anonymous check - skipping tenant verification");
            }

            // Check if already paid
            // if (bill.PaymentDate.HasValue)
            // {
            //     return ApiResponse<PaymentResponse>.Success(new PaymentResponse
            //     {
            //         Status = "Success",
            //         Message = "Hóa đơn đã được thanh toán"
            //     });
            // }

            // Lấy bank account của owner (để check transaction)
            var bankAccount = _bankAccountRepository.GetDefaultByUserId(bill.OwnerId).FirstOrDefault();
            if (bankAccount == null)
            {
                return ApiResponse<PaymentResponse>.Fail("Chủ trọ chưa thiết lập tài khoản ngân hàng");
            }
            var accountNumber = bankAccount.AccountNumber;
            
            // Generate expected content - support multiple formats
            var billIdFull = billId.ToString().ToUpper();
            var billIdLower = billId.ToString().ToLower();
            var billIdNoDash = billId.ToString().Replace("-", "").ToUpper();
            var billIdNoDashLower = billId.ToString().Replace("-", "").ToLower();
            var billCode = billId.ToString().Substring(0, 8).ToUpper();
            
            _logger.LogInformation($"🔍 Searching for transaction with Bill ID: {billId}");
            _logger.LogInformation($"📋 Account Number: {accountNumber}");
            _logger.LogInformation($"⏰ Time range: Last 24 hours from {DateTime.UtcNow}");
            
            // Try to find transaction with various content formats
            var expectedContents = new[]
            {
                // Full GUID formats
                billIdFull,                                          // "148A4D2E-8ED5-4D16-ABEA-10D3974E288F"
                billIdLower,                                         // "148a4d2e-8ed5-4d16-abea-10d3974e288f"
                billIdNoDash,                                        // "148A4D2E8ED54D16ABEA10D3974E288F"
                billIdNoDashLower,                                   // "148a4d2e8ed54d16abea10d3974e288f"
                
                // With Vietnamese text
                $"Thanh toan hoa don {billIdFull}",
                $"Thanh toan hoa don {billIdLower}",
                $"Thanh toan hoa don {billIdNoDash}",
                $"Thanh toan hoa don {billIdNoDashLower}",
                
                // Short code formats
                $"THANHTOAN BILL {billCode}",
                $"Thanh toan hoa don {billCode}",
                billCode
            };

            _logger.LogInformation($"📝 Searching with {expectedContents.Length} different content patterns");

            // Check SePay API cho giao dịch trong 24h gần đây - GỌI 1 LẦN DUY NHẤT
            _logger.LogInformation($"📡 Fetching transactions from SePay (last 24 hours)...");
            var allTransactions = await _sePayService.GetRecentTransactionsAsync(
                accountNumber,
                DateTime.UtcNow.AddHours(-24)
            );
            
            _logger.LogInformation($"📦 Received {allTransactions.Count} transactions from SePay");
            
            // Log first few transactions for debugging
            var incomingTransactions = allTransactions.Where(tx => tx.AmountInDecimal > 0).Take(5).ToList();
            _logger.LogInformation($"📋 Sample incoming transactions (first 5):");
            foreach (var tx in incomingTransactions)
            {
                _logger.LogInformation($"   - ID: {tx.Id}, Amount: {tx.AmountInDecimal}, Content: {tx.TransactionContent}");
            }
            
            // Search trong danh sách transactions với tất cả patterns
            SePayTransactionDto? transaction = null;
            string? matchedPattern = null;
            
            foreach (var expectedContent in expectedContents)
            {
                _logger.LogInformation($"🔎 Trying pattern: {expectedContent}");
                
                transaction = allTransactions.FirstOrDefault(tx => 
                    tx.TransactionContent.Contains(expectedContent, StringComparison.OrdinalIgnoreCase) &&
                    tx.AmountInDecimal > 0 // Chỉ lấy giao dịch tiền vào
                );
                
                if (transaction != null)
                {
                    matchedPattern = expectedContent;
                    _logger.LogInformation($"✅ Found transaction! Content matched: {expectedContent}");
                    _logger.LogInformation($"💰 Transaction ID: {transaction.Id}, Amount: {transaction.AmountInDecimal}");
                    _logger.LogInformation($"📝 Transaction content: {transaction.TransactionContent}");
                    break;
                }
            }

            if (transaction == null)
            {
                _logger.LogWarning($"No matching transaction found for bill {billId}");
                return ApiResponse<PaymentResponse>.Fail(
                    "Chưa tìm thấy giao dịch chuyển khoản. Vui lòng đợi vài phút hoặc kiểm tra lại nội dung chuyển khoản có đúng không."
                );
            }
            

            // Tạo payment từ transaction tìm được
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UtilityBillId = billId,
                TenantId = tenantId,
                OwnerId = bill.OwnerId,
                Amount = transaction.AmountInDecimal,
                PaymentMethod = PaymentMethod.Online,
                Status = PaymentStatus.Success,
                TransactionId = transaction.Id,
                TransactionContent = transaction.TransactionContent,
                BankAccountNumber = transaction.AccountNumber,
                BankBrandName = transaction.BankBrandName,
                CompletedDate = DateTime.Parse(transaction.TransactionDate),
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                TransactionDate = DateTime.Parse(transaction.TransactionDate)
            };

            await _paymentRepository.Add(payment);
            _logger.LogInformation($"Created payment {payment.Id} from manual check");

            // Update bill status
            await _utilityBillService.UpdateBillStatusAsync(
                billId,
                "Paid",
                DateTime.UtcNow
            );

            _logger.LogInformation($"Bill {billId} marked as paid via manual check");

            return ApiResponse<PaymentResponse>.Success(new PaymentResponse
            {
                PaymentId = payment.Id.ToString(),
                Status = "Success",
                Message = "Đã xác nhận thanh toán thành công!"
            }, "Thanh toán thành công!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in manual payment check for bill {billId}");
            return ApiResponse<PaymentResponse>.Fail($"Lỗi khi kiểm tra: {ex.Message}");
        }
    }
}
