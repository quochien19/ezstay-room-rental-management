namespace PaymentAPI.DTOs.Responses;

/// <summary>
/// Response từ SePay API khi lấy chi tiết giao dịch
/// </summary>
public class SePayTransactionResponse
{
    public bool Status { get; set; }
    public string? Message { get; set; }
    public SePayTransactionData? Data { get; set; }
}

public class SePayTransactionData
{
    public string Id { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string TransactionDate { get; set; } = string.Empty;
    public string TransferType { get; set; } = string.Empty;
    public string? ReferenceCode { get; set; }
}

/// <summary>
/// Response list giao dịch từ SePay
/// </summary>
public class SePayTransactionListResponse
{
    public bool Status { get; set; }
    public string? Message { get; set; }
    public List<SePayTransactionDto> Transactions { get; set; } = new();
}

/// <summary>
/// DTO cho 1 giao dịch SePay
/// </summary>
public class SePayTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string TransactionContent { get; set; } = string.Empty;
    public string TransferType { get; set; } = string.Empty;
    public string? ReferenceCode { get; set; }
    public string TransactionDate { get; set; } = string.Empty;
    
    // Amount as string from API
    public string? AmountIn { get; set; }
    public string? AmountOut { get; set; }
    
    // Calculated decimal amount
    public decimal AmountInDecimal => decimal.TryParse(AmountIn, out var val) ? val : 0;
    public decimal AmountOutDecimal => decimal.TryParse(AmountOut, out var val) ? val : 0;
}
