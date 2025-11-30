namespace PaymentAPI.DTOs.Requests;

public class ApprovePaymentRequest
{
    public bool IsApproved { get; set; } = true;
    public string? Notes { get; set; }
}

public class RejectPaymentRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class UploadReceiptRequest
{
    public Guid PaymentId { get; set; }
}
